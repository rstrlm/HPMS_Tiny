using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class RoomService : IRoomService
{
    private readonly PmsDbContext _context;

    public RoomService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoomDto>> GetAllAsync(bool? activeOnly = null)
    {
        var query = _context.Rooms
            .Include(r => r.RoomType)
            .AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.RoomNumber)
            .Select(r => MapToDto(r))
            .ToListAsync();
    }

    public async Task<RoomDto?> GetByIdAsync(Guid id)
    {
        var room = await _context.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);
        return room is null ? null : MapToDto(room);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomRequest request)
    {
        var room = new Room
        {
            RoomNumber = request.RoomNumber,
            RoomTypeId = request.RoomTypeId
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        await _context.Entry(room).Reference(r => r.RoomType).LoadAsync();

        return MapToDto(room);
    }

    public async Task<RoomDto?> UpdateAsync(Guid id, UpdateRoomRequest request)
    {
        var room = await _context.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
            return null;

        room.RoomNumber = request.RoomNumber;
        room.RoomTypeId = request.RoomTypeId;
        room.IsActive = request.IsActive;
        room.CurrentStatus = request.CurrentStatus;

        await _context.SaveChangesAsync();

        await _context.Entry(room).Reference(r => r.RoomType).LoadAsync();

        return MapToDto(room);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null)
            return false;

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return true;
    }

    private static RoomDto MapToDto(Room r) => new(
        r.Id,
        r.RoomNumber,
        r.RoomTypeId,
        r.RoomType?.Name,
        r.IsActive,
        r.CurrentStatus,
        r.CreatedAtUtc,
        r.UpdatedAtUtc);
}
