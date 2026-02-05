using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Pms.Application.DTOs;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class ReservationServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<(RoomType, Room, Room, Customer)> SeedTestData(PmsDbContext context)
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

        var room1 = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = "101",
            RoomTypeId = roomType.Id,
            IsActive = true,
            CurrentStatus = RoomStatus.Available,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Rooms.Add(room1);

        var room2 = new Room
        {
            Id = Guid.NewGuid(),
            RoomNumber = "102",
            RoomTypeId = roomType.Id,
            IsActive = true,
            CurrentStatus = RoomStatus.Available,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Rooms.Add(room2);

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
        return (roomType, room1, room2, customer);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateReservationWithAssignments()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        var request = new CreateReservationRequest(
            customer.Id,
            checkIn,
            checkOut,
            NumberOfGuests: 2,
            Notes: "Test reservation",
            RoomAssignments: new[]
            {
                new CreateRoomAssignmentRequest(room1.Id, checkIn, checkOut)
            });

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(ReservationStatus.Confirmed, result.Status);
        Assert.Single(result.RoomAssignments);
        Assert.Equal(room1.Id, result.RoomAssignments.First().RoomId);
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenRoomNotAvailable()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        // Create first reservation
        var firstRequest = new CreateReservationRequest(
            customer.Id, checkIn, checkOut, 2, null,
            new[] { new CreateRoomAssignmentRequest(room1.Id, checkIn, checkOut) });
        await service.CreateAsync(firstRequest);

        // Try to create second overlapping reservation
        var secondRequest = new CreateReservationRequest(
            customer.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            2, null,
            new[] { new CreateRoomAssignmentRequest(room1.Id, checkIn.AddDays(1), checkOut.AddDays(1)) });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(secondRequest));
    }

    [Fact]
    public async Task CreateAsync_ShouldFail_WhenInvalidDates()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var request = new CreateReservationRequest(
            customer.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)), // Check-in after check-out
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            2, null,
            new[] { new CreateRoomAssignmentRequest(room1.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), DateOnly.FromDateTime(DateTime.Today.AddDays(3))) });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ShouldSupportMultiRoomReservation()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, room2, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        var request = new CreateReservationRequest(
            customer.Id, checkIn, checkOut, 4, "Family booking",
            new[]
            {
                new CreateRoomAssignmentRequest(room1.Id, checkIn, checkOut),
                new CreateRoomAssignmentRequest(room2.Id, checkIn, checkOut)
            });

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Equal(2, result.RoomAssignments.Count());
        Assert.Contains(result.RoomAssignments, a => a.RoomId == room1.Id);
        Assert.Contains(result.RoomAssignments, a => a.RoomId == room2.Id);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToCheckedIn_ShouldUpdateRoomStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today);
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(2));

        var request = new CreateReservationRequest(
            customer.Id, checkIn, checkOut, 2, null,
            new[] { new CreateRoomAssignmentRequest(room1.Id, checkIn, checkOut) });
        var reservation = await service.CreateAsync(request);

        // Act
        await service.ChangeStatusAsync(reservation.Id, ReservationStatus.CheckedIn);

        // Assert
        var room = await context.Rooms.FindAsync(room1.Id);
        Assert.Equal(RoomStatus.Occupied, room!.CurrentStatus);
    }

    [Fact]
    public async Task ChangeStatusAsync_ToCheckedOut_ShouldSetRoomNeedsCleaning()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, _, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today);
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(2));

        var request = new CreateReservationRequest(
            customer.Id, checkIn, checkOut, 2, null,
            new[] { new CreateRoomAssignmentRequest(room1.Id, checkIn, checkOut) });
        var reservation = await service.CreateAsync(request);
        await service.ChangeStatusAsync(reservation.Id, ReservationStatus.CheckedIn);

        // Act
        await service.ChangeStatusAsync(reservation.Id, ReservationStatus.CheckedOut);

        // Assert
        var room = await context.Rooms.FindAsync(room1.Id);
        Assert.Equal(RoomStatus.NeedsCleaning, room!.CurrentStatus);
    }

    [Fact]
    public async Task AddRoomAssignmentAsync_ShouldAddToExistingReservation()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, room2, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        var request = new CreateReservationRequest(
            customer.Id, checkIn, checkOut, 2, null,
            new[] { new CreateRoomAssignmentRequest(room1.Id, checkIn, checkOut) });
        var reservation = await service.CreateAsync(request);

        // Act
        var newAssignment = await service.AddRoomAssignmentAsync(
            reservation.Id,
            new CreateRoomAssignmentRequest(room2.Id, checkIn, checkOut));

        // Assert
        Assert.NotNull(newAssignment);
        Assert.Equal(room2.Id, newAssignment.RoomId);

        var updated = await service.GetByIdAsync(reservation.Id);
        Assert.Equal(2, updated!.RoomAssignments.Count());
    }

    [Fact]
    public async Task RemoveRoomAssignmentAsync_ShouldRemoveAssignment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room1, room2, customer) = await SeedTestData(context);
        var availabilityService = new RoomAvailabilityService(context);
        var housekeepingService = new HousekeepingService(context);
        var service = new ReservationService(context, availabilityService, housekeepingService);

        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        var request = new CreateReservationRequest(
            customer.Id, checkIn, checkOut, 4, null,
            new[]
            {
                new CreateRoomAssignmentRequest(room1.Id, checkIn, checkOut),
                new CreateRoomAssignmentRequest(room2.Id, checkIn, checkOut)
            });
        var reservation = await service.CreateAsync(request);
        var assignmentToRemove = reservation.RoomAssignments.First();

        // Act
        var result = await service.RemoveRoomAssignmentAsync(assignmentToRemove.Id);

        // Assert
        Assert.True(result);
        var updated = await service.GetByIdAsync(reservation.Id);
        Assert.Single(updated!.RoomAssignments);
    }
}
