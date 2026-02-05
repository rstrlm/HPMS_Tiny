using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;
using Pms.Infrastructure.Services;

namespace Pms.Tests.Services;

public class RoomStateBlockServiceTests
{
    private static PmsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PmsDbContext(options);
    }

    private static async Task<Room> CreateRoom(PmsDbContext context)
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
        await context.SaveChangesAsync();
        return room;
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateMaintenanceBlock()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var room = await CreateRoom(context);
        var service = new RoomStateBlockService(context);
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(3);
        var request = new CreateRoomStateBlockRequest(startDate, endDate, RoomStateBlockType.Maintenance, "Plumbing repair");

        // Act
        var result = await service.CreateAsync(room.Id, request);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(room.Id, result.RoomId);
        Assert.Equal(RoomStateBlockType.Maintenance, result.Type);
        Assert.Equal("Plumbing repair", result.Note);
    }

    [Fact]
    public async Task GetByRoomAsync_ShouldFilterByDateRange()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var room = await CreateRoom(context);
        var service = new RoomStateBlockService(context);

        var now = DateTime.UtcNow;
        await service.CreateAsync(room.Id, new CreateRoomStateBlockRequest(now, now.AddDays(2), RoomStateBlockType.Maintenance, "Block 1"));
        await service.CreateAsync(room.Id, new CreateRoomStateBlockRequest(now.AddDays(10), now.AddDays(12), RoomStateBlockType.OutOfService, "Block 2"));

        // Act
        var blocksInRange = await service.GetByRoomAsync(room.Id, now.AddDays(-1), now.AddDays(5));
        var allBlocks = await service.GetByRoomAsync(room.Id);

        // Assert
        Assert.Single(blocksInRange);
        Assert.Equal(2, allBlocks.Count());
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveBlock()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var room = await CreateRoom(context);
        var service = new RoomStateBlockService(context);
        var block = await service.CreateAsync(room.Id, new CreateRoomStateBlockRequest(DateTime.UtcNow, DateTime.UtcNow.AddDays(1), RoomStateBlockType.Maintenance, null));

        // Act
        var deleted = await service.DeleteAsync(block.Id);
        var fetched = await service.GetByIdAsync(block.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(fetched);
    }
}
