using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class TreatmentAvailabilityServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<(TreatmentRoom, TreatmentType, StaffProfile, Customer)> SeedTestData(PmsDbContext context)
    {
        var treatmentRoom = new TreatmentRoom
        {
            Id = Guid.NewGuid(),
            Name = "Sauna",
            Capacity = 5, // Can fit 5 people at once
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

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test Customer",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        await context.SaveChangesAsync();
        return (treatmentRoom, treatmentType, therapist, customer);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrue_WhenCapacityAvailable()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, _, customer) = await SeedTestData(context);
        var service = new TreatmentAvailabilityService(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Act
        var result = await service.IsRoomAvailableAsync(room.Id, startTime, endTime, seatsNeeded: 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrue_WhenPartialCapacityUsed()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, _, customer) = await SeedTestData(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Add an appointment using 3 seats (room capacity is 5)
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 3,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TreatmentAvailabilityService(context);

        // Act - Request 2 more seats (3 + 2 = 5 <= 5 capacity)
        var result = await service.IsRoomAvailableAsync(room.Id, startTime, endTime, seatsNeeded: 2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnFalse_WhenCapacityExceeded()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, _, customer) = await SeedTestData(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Add an appointment using 4 seats
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 4,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TreatmentAvailabilityService(context);

        // Act - Request 2 more seats (4 + 2 = 6 > 5 capacity)
        var result = await service.IsRoomAvailableAsync(room.Id, startTime, endTime, seatsNeeded: 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldIgnoreCancelledAppointments()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, _, customer) = await SeedTestData(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Add a CANCELLED appointment using all 5 seats
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 5,
            Status = AppointmentStatus.Cancelled, // Cancelled!
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TreatmentAvailabilityService(context);

        // Act - Should be available since the appointment is cancelled
        var result = await service.IsRoomAvailableAsync(room.Id, startTime, endTime, seatsNeeded: 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrue_WhenNonOverlappingTimes()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, _, customer) = await SeedTestData(context);

        var existingStart = DateTime.UtcNow.AddHours(1);
        var existingEnd = existingStart.AddMinutes(60);

        // Add an appointment using all capacity
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = existingStart,
            EndAtUtc = existingEnd,
            SeatsUsed = 5,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TreatmentAvailabilityService(context);

        // Act - Request time slot AFTER the existing one
        var newStart = existingEnd.AddMinutes(15);
        var newEnd = newStart.AddMinutes(60);
        var result = await service.IsRoomAvailableAsync(room.Id, newStart, newEnd, seatsNeeded: 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTherapistAvailableAsync_ShouldReturnTrue_WhenNoOverlap()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, therapist, customer) = await SeedTestData(context);
        var service = new TreatmentAvailabilityService(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Act
        var result = await service.IsTherapistAvailableAsync(therapist.Id, startTime, endTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTherapistAvailableAsync_ShouldReturnFalse_WhenOverlapping()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, therapist, customer) = await SeedTestData(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Add an appointment with the therapist
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            TherapistStaffId = therapist.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 1,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TreatmentAvailabilityService(context);

        // Act - Try to book same therapist at overlapping time
        var result = await service.IsTherapistAvailableAsync(therapist.Id, startTime.AddMinutes(30), endTime.AddMinutes(30));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetRoomOccupancyAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, _, customer) = await SeedTestData(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Add two appointments: 2 seats + 1 seat = 3 total
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 2,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        context.TreatmentAppointments.Add(new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 1,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new TreatmentAvailabilityService(context);

        // Act
        var occupancy = await service.GetRoomOccupancyAsync(room.Id, startTime.AddMinutes(30));

        // Assert
        Assert.Equal(3, occupancy);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldExcludeSpecifiedAppointment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (room, treatmentType, _, customer) = await SeedTestData(context);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddMinutes(60);

        // Add an appointment using all capacity
        var existingAppointment = new TreatmentAppointment
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            TreatmentTypeId = treatmentType.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = startTime,
            EndAtUtc = endTime,
            SeatsUsed = 5,
            Status = AppointmentStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.TreatmentAppointments.Add(existingAppointment);
        await context.SaveChangesAsync();

        var service = new TreatmentAvailabilityService(context);

        // Act - Check availability excluding the existing appointment (for update scenario)
        var result = await service.IsRoomAvailableAsync(room.Id, startTime, endTime, seatsNeeded: 5, excludeAppointmentId: existingAppointment.Id);

        // Assert
        Assert.True(result);
    }
}
