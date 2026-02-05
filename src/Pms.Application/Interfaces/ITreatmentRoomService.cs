using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface ITreatmentRoomService
{
    Task<IEnumerable<TreatmentRoomDto>> GetAllAsync(bool? activeOnly = null);
    Task<TreatmentRoomDto?> GetByIdAsync(Guid id);
    Task<TreatmentRoomDto> CreateAsync(CreateTreatmentRoomRequest request);
    Task<TreatmentRoomDto?> UpdateAsync(Guid id, UpdateTreatmentRoomRequest request);
    Task<bool> DeleteAsync(Guid id);
}
