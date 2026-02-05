using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class RoomServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<RoomType> CreateRoomType(PmsDbContext context)
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
        await context.SaveChangesAsync();
        return roomType;
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateRoom()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var roomType = await CreateRoomType(context);
        var service = new RoomService(context);
        var request = new CreateRoomRequest("101", roomType.Id);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("101", result.RoomNumber);
        Assert.Equal(roomType.Id, result.RoomTypeId);
        Assert.True(result.IsActive);
        Assert.Equal(RoomStatus.Available, result.CurrentStatus);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterActiveRooms()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var roomType = await CreateRoomType(context);
        var service = new RoomService(context);

        var room1 = await service.CreateAsync(new CreateRoomRequest("101", roomType.Id));
        await service.CreateAsync(new CreateRoomRequest("102", roomType.Id));
        await service.UpdateAsync(room1.Id, new UpdateRoomRequest("101", roomType.Id, false, RoomStatus.OutOfService));

        // Act
        var activeRooms = await service.GetAllAsync(activeOnly: true);
        var allRooms = await service.GetAllAsync();

        // Assert
        Assert.Single(activeRooms);
        Assert.Equal(2, allRooms.Count());
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeRoomStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var roomType = await CreateRoomType(context);
        var service = new RoomService(context);
        var room = await service.CreateAsync(new CreateRoomRequest("101", roomType.Id));

        // Act
        var updated = await service.UpdateAsync(room.Id, new UpdateRoomRequest("101", roomType.Id, true, RoomStatus.Occupied));

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(RoomStatus.Occupied, updated.CurrentStatus);
    }
}
