using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class TreatmentRoomService : ITreatmentRoomService
{
    private readonly PmsDbContext _context;

    public TreatmentRoomService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TreatmentRoomDto>> GetAllAsync(bool? activeOnly = null)
    {
        var query = _context.TreatmentRooms.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(tr => tr.IsActive);
        }

        return await query
            .OrderBy(tr => tr.Name)
            .Select(tr => MapToDto(tr))
            .ToListAsync();
    }

    public async Task<TreatmentRoomDto?> GetByIdAsync(Guid id)
    {
        var treatmentRoom = await _context.TreatmentRooms.FindAsync(id);
        return treatmentRoom is null ? null : MapToDto(treatmentRoom);
    }

    public async Task<TreatmentRoomDto> CreateAsync(CreateTreatmentRoomRequest request)
    {
        var treatmentRoom = new TreatmentRoom
        {
            Name = request.Name,
            Description = request.Description,
            Capacity = request.Capacity
        };

        _context.TreatmentRooms.Add(treatmentRoom);
        await _context.SaveChangesAsync();

        return MapToDto(treatmentRoom);
    }

    public async Task<TreatmentRoomDto?> UpdateAsync(Guid id, UpdateTreatmentRoomRequest request)
    {
        var treatmentRoom = await _context.TreatmentRooms.FindAsync(id);
        if (treatmentRoom is null)
            return null;

        treatmentRoom.Name = request.Name;
        treatmentRoom.Description = request.Description;
        treatmentRoom.Capacity = request.Capacity;
        treatmentRoom.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(treatmentRoom);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var treatmentRoom = await _context.TreatmentRooms.FindAsync(id);
        if (treatmentRoom is null)
            return false;

        _context.TreatmentRooms.Remove(treatmentRoom);
        await _context.SaveChangesAsync();

        return true;
    }

    private static TreatmentRoomDto MapToDto(TreatmentRoom tr) => new(
        tr.Id,
        tr.Name,
        tr.Description,
        tr.Capacity,
        tr.IsActive);
}
