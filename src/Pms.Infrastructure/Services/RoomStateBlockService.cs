using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class RoomStateBlockService : IRoomStateBlockService
{
    private readonly PmsDbContext _context;

    public RoomStateBlockService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoomStateBlockDto>> GetByRoomAsync(Guid roomId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.RoomStateBlocks
            .Include(rsb => rsb.Room)
            .Include(rsb => rsb.CreatedByStaff)
            .Where(rsb => rsb.RoomId == roomId);

        if (from.HasValue)
        {
            query = query.Where(rsb => rsb.EndAtUtc > from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(rsb => rsb.StartAtUtc < to.Value);
        }

        return await query
            .OrderBy(rsb => rsb.StartAtUtc)
            .Select(rsb => MapToDto(rsb))
            .ToListAsync();
    }

    public async Task<RoomStateBlockDto?> GetByIdAsync(Guid id)
    {
        var block = await _context.RoomStateBlocks
            .Include(rsb => rsb.Room)
            .Include(rsb => rsb.CreatedByStaff)
            .FirstOrDefaultAsync(rsb => rsb.Id == id);

        return block is null ? null : MapToDto(block);
    }

    public async Task<RoomStateBlockDto> CreateAsync(Guid roomId, CreateRoomStateBlockRequest request, Guid? staffId = null)
    {
        var block = new RoomStateBlock
        {
            RoomId = roomId,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = request.EndAtUtc,
            Type = request.Type,
            Note = request.Note,
            CreatedByStaffId = staffId
        };

        _context.RoomStateBlocks.Add(block);
        await _context.SaveChangesAsync();

        await _context.Entry(block).Reference(rsb => rsb.Room).LoadAsync();
        if (staffId.HasValue)
        {
            await _context.Entry(block).Reference(rsb => rsb.CreatedByStaff).LoadAsync();
        }

        return MapToDto(block);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var block = await _context.RoomStateBlocks.FindAsync(id);
        if (block is null)
            return false;

        _context.RoomStateBlocks.Remove(block);
        await _context.SaveChangesAsync();

        return true;
    }

    private static RoomStateBlockDto MapToDto(RoomStateBlock rsb) => new(
        rsb.Id,
        rsb.RoomId,
        rsb.Room?.RoomNumber,
        rsb.StartAtUtc,
        rsb.EndAtUtc,
        rsb.Type,
        rsb.Note,
        rsb.CreatedByStaffId,
        rsb.CreatedByStaff?.DisplayName,
        rsb.CreatedAtUtc);
}
