using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface IRoomTypeService
{
    Task<IEnumerable<RoomTypeDto>> GetAllAsync();
    Task<RoomTypeDto?> GetByIdAsync(Guid id);
    Task<RoomTypeDto> CreateAsync(CreateRoomTypeRequest request);
    Task<RoomTypeDto?> UpdateAsync(Guid id, UpdateRoomTypeRequest request);
    Task<bool> DeleteAsync(Guid id);
}
