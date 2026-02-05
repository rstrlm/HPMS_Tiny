using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly PmsDbContext _context;

    public RoomTypeService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoomTypeDto>> GetAllAsync()
    {
        return await _context.RoomTypes
            .OrderBy(rt => rt.Name)
            .Select(rt => MapToDto(rt))
            .ToListAsync();
    }

    public async Task<RoomTypeDto?> GetByIdAsync(Guid id)
    {
        var roomType = await _context.RoomTypes.FindAsync(id);
        return roomType is null ? null : MapToDto(roomType);
    }

    public async Task<RoomTypeDto> CreateAsync(CreateRoomTypeRequest request)
    {
        var roomType = new RoomType
        {
            Name = request.Name,
            Description = request.Description,
            Capacity = request.Capacity,
            BasePrice = request.BasePrice
        };

        _context.RoomTypes.Add(roomType);
        await _context.SaveChangesAsync();

        return MapToDto(roomType);
    }

    public async Task<RoomTypeDto?> UpdateAsync(Guid id, UpdateRoomTypeRequest request)
    {
        var roomType = await _context.RoomTypes.FindAsync(id);
        if (roomType is null)
            return null;

        roomType.Name = request.Name;
        roomType.Description = request.Description;
        roomType.Capacity = request.Capacity;
        roomType.BasePrice = request.BasePrice;

        await _context.SaveChangesAsync();

        return MapToDto(roomType);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var roomType = await _context.RoomTypes.FindAsync(id);
        if (roomType is null)
            return false;

        _context.RoomTypes.Remove(roomType);
        await _context.SaveChangesAsync();

        return true;
    }

    private static RoomTypeDto MapToDto(RoomType rt) => new(
        rt.Id,
        rt.Name,
        rt.Description,
        rt.Capacity,
        rt.BasePrice,
        rt.CreatedAtUtc,
        rt.UpdatedAtUtc);
}
