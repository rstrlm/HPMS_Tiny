using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class TreatmentTypeService : ITreatmentTypeService
{
    private readonly PmsDbContext _context;

    public TreatmentTypeService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TreatmentTypeDto>> GetAllAsync(bool? activeOnly = null)
    {
        var query = _context.TreatmentTypes.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(tt => tt.IsActive);
        }

        return await query
            .OrderBy(tt => tt.Name)
            .Select(tt => MapToDto(tt))
            .ToListAsync();
    }

    public async Task<TreatmentTypeDto?> GetByIdAsync(Guid id)
    {
        var treatmentType = await _context.TreatmentTypes.FindAsync(id);
        return treatmentType is null ? null : MapToDto(treatmentType);
    }

    public async Task<TreatmentTypeDto> CreateAsync(CreateTreatmentTypeRequest request)
    {
        var treatmentType = new TreatmentType
        {
            Name = request.Name,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            BufferMinutes = request.BufferMinutes,
            BasePrice = request.BasePrice,
            RequiresTherapist = request.RequiresTherapist
        };

        _context.TreatmentTypes.Add(treatmentType);
        await _context.SaveChangesAsync();

        return MapToDto(treatmentType);
    }

    public async Task<TreatmentTypeDto?> UpdateAsync(Guid id, UpdateTreatmentTypeRequest request)
    {
        var treatmentType = await _context.TreatmentTypes.FindAsync(id);
        if (treatmentType is null)
            return null;

        treatmentType.Name = request.Name;
        treatmentType.Description = request.Description;
        treatmentType.DurationMinutes = request.DurationMinutes;
        treatmentType.BufferMinutes = request.BufferMinutes;
        treatmentType.BasePrice = request.BasePrice;
        treatmentType.IsActive = request.IsActive;
        treatmentType.RequiresTherapist = request.RequiresTherapist;

        await _context.SaveChangesAsync();

        return MapToDto(treatmentType);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var treatmentType = await _context.TreatmentTypes.FindAsync(id);
        if (treatmentType is null)
            return false;

        _context.TreatmentTypes.Remove(treatmentType);
        await _context.SaveChangesAsync();

        return true;
    }

    private static TreatmentTypeDto MapToDto(TreatmentType tt) => new(
        tt.Id,
        tt.Name,
        tt.Description,
        tt.DurationMinutes,
        tt.BufferMinutes,
        tt.BasePrice,
        tt.IsActive,
        tt.RequiresTherapist);
}
