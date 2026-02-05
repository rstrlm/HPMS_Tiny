using Microsoft.EntityFrameworkCore;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class RoomAvailabilityServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<(RoomType, Room, Customer)> SeedBasicData(PmsDbContext context)
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
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Customers.Add(customer);

        await context.SaveChangesAsync();
        return (roomType, room, customer);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrue_WhenNoConflicts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, _) = await SeedBasicData(context);
        var service = new RoomAvailabilityService(context);

        // Act
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnFalse_WhenInactiveRoom()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, _) = await SeedBasicData(context);
        room.IsActive = false;
        await context.SaveChangesAsync();
        var service = new RoomAvailabilityService(context);

        // Act
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnFalse_WhenOverlappingAssignment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, customer) = await SeedBasicData(context);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            Status = ReservationStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        var assignment = new RoomAssignment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            RoomId = room.Id,
            FromDate = reservation.CheckInDate,
            ToDate = reservation.CheckOutDate,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var service = new RoomAvailabilityService(context);

        // Act - Request overlapping dates
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrue_WhenAssignmentIsCancelled()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, customer) = await SeedBasicData(context);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            Status = ReservationStatus.Cancelled, // Cancelled!
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        var assignment = new RoomAssignment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            RoomId = room.Id,
            FromDate = reservation.CheckInDate,
            ToDate = reservation.CheckOutDate,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var service = new RoomAvailabilityService(context);

        // Act - Cancelled reservations should not block
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnFalse_WhenMaintenanceBlock()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, _) = await SeedBasicData(context);

        var block = new RoomStateBlock
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            EndAtUtc = DateTime.UtcNow.AddDays(4),
            Type = RoomStateBlockType.Maintenance,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomStateBlocks.Add(block);
        await context.SaveChangesAsync();

        var service = new RoomAvailabilityService(context);

        // Act
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnFalse_WhenActiveHold()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, _) = await SeedBasicData(context);

        var hold = new ReservationHold
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            FromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10), // Not expired
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.ReservationHolds.Add(hold);
        await context.SaveChangesAsync();

        var service = new RoomAvailabilityService(context);

        // Act
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrue_WhenHoldIsExpired()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, _) = await SeedBasicData(context);

        var hold = new ReservationHold
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            FromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5), // Expired!
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.ReservationHolds.Add(hold);
        await context.SaveChangesAsync();

        var service = new RoomAvailabilityService(context);

        // Act
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRoomAvailableAsync_ShouldReturnTrue_WhenExcludingOwnReservation()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, customer) = await SeedBasicData(context);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            Status = ReservationStatus.Confirmed,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.Reservations.Add(reservation);

        var assignment = new RoomAssignment
        {
            Id = Guid.NewGuid(),
            ReservationId = reservation.Id,
            RoomId = room.Id,
            FromDate = reservation.CheckInDate,
            ToDate = reservation.CheckOutDate,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.RoomAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var service = new RoomAvailabilityService(context);

        // Act - Exclude own reservation (for update scenarios)
        var result = await service.IsRoomAvailableAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            excludeReservationId: reservation.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PlaceHoldAsync_ShouldCreateHold()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, _) = await SeedBasicData(context);
        var service = new RoomAvailabilityService(context);

        // Act
        var holdId = await service.PlaceHoldAsync(
            room.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            holdMinutes: 10);

        // Assert
        Assert.NotEqual(Guid.Empty, holdId);
        var hold = await context.ReservationHolds.FindAsync(holdId);
        Assert.NotNull(hold);
        Assert.Equal(room.Id, hold.RoomId);
    }

    [Fact]
    public async Task CleanupExpiredHoldsAsync_ShouldRemoveExpiredHolds()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var (_, room, _) = await SeedBasicData(context);

        // Add expired hold
        context.ReservationHolds.Add(new ReservationHold
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            FromDate = DateOnly.FromDateTime(DateTime.Today),
            ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        // Add active hold
        context.ReservationHolds.Add(new ReservationHold
        {
            Id = Guid.NewGuid(),
            RoomId = room.Id,
            FromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(6)),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new RoomAvailabilityService(context);

        // Act
        var cleaned = await service.CleanupExpiredHoldsAsync();

        // Assert
        Assert.Equal(1, cleaned);
        Assert.Single(context.ReservationHolds);
    }
}
