using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface ITreatmentTypeService
{
    Task<IEnumerable<TreatmentTypeDto>> GetAllAsync(bool? activeOnly = null);
    Task<TreatmentTypeDto?> GetByIdAsync(Guid id);
    Task<TreatmentTypeDto> CreateAsync(CreateTreatmentTypeRequest request);
    Task<TreatmentTypeDto?> UpdateAsync(Guid id, UpdateTreatmentTypeRequest request);
    Task<bool> DeleteAsync(Guid id);
}
