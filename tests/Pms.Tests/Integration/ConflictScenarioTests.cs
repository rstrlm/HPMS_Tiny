using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pms.Application.DTOs;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Integration;

/// <summary>
/// Integration tests for conflict scenarios - room double-booking and treatment capacity.
/// These tests verify that the system correctly prevents overbooking.
/// </summary>
public class ConflictScenarioTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<(RoomType, Room, Customer)> SeedRoomData(PmsDbContext context)
    {
        var roomType = new RoomType
        {
            Id = Guid.NewGuid(),
            Name = "Standard",
            Capacity = 2,
            BasePrice = 100.00m,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomTypes.Add(roomType);

        var room = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = "101",
            RoomTypeId = roomType.Id,
            IsActive = true,
            CurrentStatus = RoomStatus.Available,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Rooms.Add(room);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            Email = "test@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        await context.SaveChangesAsync();
        return (roomType, room, customer);
    }

    private static async Task<(TreatmentRoom, TreatmentType, Customer)> SeedTreatmentData(PmsDbContext context)
    {
        var treatmentRoom = new TreatmentRoom
        {
            Id = Guid.NewGuid(),
            Name = "Sauna",
            Capacity = 3, // Can fit 3 people
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.TreatmentRooms.Add(treatmentRoom);

        var treatmentType = new TreatmentType
        {
            Id = Guid.NewGuid(),
            Name = "Sauna Session",
            DurationMinutes = 60,
            BufferMinutes = 0,
            BasePrice = 25.00m,
            IsActive = true,
            RequiresTherapist = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.TreatmentTypes.Add(treatmentType);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            Email = "test@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        await context.SaveChangesAsync();
        return (treatmentRoom, treatmentType, customer);
    }

    #region Room Double-Booking Tests

    [Fact]
    public async Task RoomReservation_ConcurrentBookings_ShouldPreventDoubleBooking()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, customer) = await SeedRoomData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var reservationService = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        // Act - Create first reservation
        var firstRequest = new CreateReservationRequest(
            customer.Id, checkIn, checkOut, 2, "First booking",
            new[] { new CreateRoomAssignmentRequest(room.Id, checkIn, checkOut) });
        var firstReservation = await reservationService.CreateAsync(firstRequest);

        // Act - Try to create second overlapping reservation
        var secondRequest = new CreateReservationRequest(
            customer.Id, checkIn.AddDays(1), checkOut.AddDays(1), 2, "Second booking",
            new[] { new CreateRoomAssignmentRequest(room.Id, checkIn.AddDays(1), checkOut.AddDays(1)) });

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reservationService.CreateAsync(secondRequest));
        Assert.Contains("not available", exception.Message);
    }

    [Fact]
    public async Task RoomReservation_AdjacentBookings_ShouldSucceed()
    {
        // Arrange - Bookings that touch but don't overlap should be allowed
        using var context = CreateInMemoryContext();
        var (_, room, customer) = await SeedRoomData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var reservationService = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn1 = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut1 = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        // Create first reservation
        var firstRequest = new CreateReservationRequest(
            customer.Id, checkIn1, checkOut1, 2, null,
            new[] { new CreateRoomAssignmentRequest(room.Id, checkIn1, checkOut1) });
        await reservationService.CreateAsync(firstRequest);

        // Create second reservation starting on checkout day
        var checkIn2 = checkOut1; // Same day as checkout
        var checkOut2 = checkOut1.AddDays(2);
        var secondRequest = new CreateReservationRequest(
            customer.Id, checkIn2, checkOut2, 2, null,
            new[] { new CreateRoomAssignmentRequest(room.Id, checkIn2, checkOut2) });

        // Act
        var secondReservation = await reservationService.CreateAsync(secondRequest);

        // Assert
        Assert.NotNull(secondReservation);
        Assert.Equal(checkIn2, secondReservation.CheckInDate);
    }

    [Fact]
    public async Task RoomReservation_MaintenanceBlock_ShouldPreventBooking()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, customer) = await SeedRoomData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var reservationService = new ReservationService(context, availabilityService, housekeepingService);

        var blockStart = DateTime.UtcNow.Date.AddDays(1);
        var blockEnd = DateTime.UtcNow.Date.AddDays(5);

        // Create maintenance block
        context.RoomStateBlocks.Add(new RoomStateBlock
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            StartAtUtc = blockStart,
            EndAtUtc = blockEnd,
            Type = RoomStateBlockType.Maintenance,
            Note = "Renovation",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act - Try to book during maintenance
        var reservationStart = DateOnly.FromDateTime(blockStart.AddDays(1));
        var reservationEnd = DateOnly.FromDateTime(blockEnd.AddDays(-1));
        var request = new CreateReservationRequest(
            customer.Id, reservationStart, reservationEnd, 2, null,
            new[] { new CreateRoomAssignmentRequest(room.Id, reservationStart, reservationEnd) });

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reservationService.CreateAsync(request));
        Assert.Contains("not available", exception.Message);
    }

    #endregion

    #region Treatment Capacity Tests

    [Fact]
    public async Task TreatmentRoom_ExactCapacity_ShouldSucceed()
    {
        // Arrange - Sauna with capacity 3
        using var context = CreateInMemoryContext();
        var (treatmentRoom, treatmentType, customer) = await SeedTreatmentData(context);
        var availabilityService = new TreatmentAvailabilityService(context);

        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddMinutes(60);

        // Add appointment using 2 seats
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = treatmentRoom.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 2,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act - Check if 1 more seat is available (2 + 1 = 3 = capacity)
        var isAvailable = await availabilityService.IsRoomAvailableAsync(
            treatmentRoom.Id, startTime, endTime, seatsNeeded: 1);

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task TreatmentRoom_OverCapacity_ShouldFail()
    {
        // Arrange - Sauna with capacity 3
        using var context = CreateInMemoryContext();
        var (treatmentRoom, treatmentType, customer) = await SeedTreatmentData(context);
        var availabilityService = new TreatmentAvailabilityService(context);

        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddMinutes(60);

        // Add appointment using 2 seats
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = treatmentRoom.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 2,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act - Check if 2 more seats are available (2 + 2 = 4 > 3 capacity)
        var isAvailable = await availabilityService.IsRoomAvailableAsync(
            treatmentRoom.Id, startTime, endTime, seatsNeeded: 2);

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task TreatmentRoom_MultipleOverlappingAppointments_ShouldRespectCapacity()
    {
        // Arrange - Sauna with capacity 3
        using var context = CreateInMemoryContext();
        var (treatmentRoom, treatmentType, customer) = await SeedTreatmentData(context);
        var availabilityService = new TreatmentAvailabilityService(context);

        var baseStart = DateTime.UtcNow.AddHours(2);

        // Appointment 1: 14:00-15:00, 1 seat
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = treatmentRoom.Id,
            StartAtUtc = baseStart,
            EndAtUtc = baseStart.AddMinutes(60),
            SeatsUsed = 1,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        // Appointment 2: 14:30-15:30, 1 seat (overlaps with both time slots)
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = treatmentRoom.Id,
            StartAtUtc = baseStart.AddMinutes(30),
            EndAtUtc = baseStart.AddMinutes(90),
            SeatsUsed = 1,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act - Check availability at 14:30 (when both appointments overlap)
        // Currently: 1 + 1 = 2 seats used
        // Available: 3 - 2 = 1 seat
        var occupancy = await availabilityService.GetRoomOccupancyAsync(
            treatmentRoom.Id, baseStart.AddMinutes(45));

        // Assert
        Assert.Equal(2, occupancy);

        // Should be able to book 1 more seat
        Assert.True(await availabilityService.IsRoomAvailableAsync(
            treatmentRoom.Id, baseStart.AddMinutes(30), baseStart.AddMinutes(60), seatsNeeded: 1));

        // Should NOT be able to book 2 more seats
        Assert.False(await availabilityService.IsRoomAvailableAsync(
            treatmentRoom.Id, baseStart.AddMinutes(30), baseStart.AddMinutes(60), seatsNeeded: 2));
    }

    [Fact]
    public async Task TreatmentRoom_CancelledAppointment_ShouldFreeCapacity()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (treatmentRoom, treatmentType, customer) = await SeedTreatmentData(context);
        var availabilityService = new TreatmentAvailabilityService(context);

        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddMinutes(60);

        // Add CANCELLED appointment using all 3 seats
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = treatmentRoom.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 3,
            Status = AppointmentStatus.Cancelled, // Cancelled!
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act - Should be available since appointment is cancelled
        var isAvailable = await availabilityService.IsRoomAvailableAsync(
            treatmentRoom.Id, startTime, endTime, seatsNeeded: 3);

        // Assert
        Assert.True(isAvailable);
    }

    #endregion

    #region Therapist Availability Tests

    [Fact]
    public async Task TherapistAvailability_Overlap_ShouldPreventDoubleBooking()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (treatmentRoom, treatmentType, customer) = await SeedTreatmentData(context);
        var availabilityService = new TreatmentAvailabilityService(context);

        var therapist = new StaffProfile
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = "therapist-1",
            DisplayName = "Test Therapist",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.StaffProfiles.Add(therapist);

        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddMinutes(60);

        // Add appointment for therapist
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = treatmentRoom.Id,
            TherapistStaffId = therapist.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 1,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act - Check therapist availability for overlapping time
        var isAvailable = await availabilityService.IsTherapistAvailableAsync(
            therapist.Id, startTime.AddMinutes(30), endTime.AddMinutes(30));

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task TherapistAvailability_NonOverlapping_ShouldSucceed()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (treatmentRoom, treatmentType, customer) = await SeedTreatmentData(context);
        var availabilityService = new TreatmentAvailabilityService(context);

        var therapist = new StaffProfile
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = "therapist-1",
            DisplayName = "Test Therapist",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.StaffProfiles.Add(therapist);

        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = startTime.AddMinutes(60);

        // Add appointment for therapist
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = treatmentRoom.Id,
            TherapistStaffId = therapist.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 1,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act - Check therapist availability for AFTER the existing appointment
        var isAvailable = await availabilityService.IsTherapistAvailableAsync(
            therapist.Id, endTime.AddMinutes(15), endTime.AddMinutes(75));

        // Assert
        Assert.True(isAvailable);
    }

    #endregion
}
