using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface IRoomService
{
    Task<IEnumerable<RoomDto>> GetAllAsync(bool? activeOnly = null);
    Task<RoomDto?> GetByIdAsync(Guid id);
    Task<RoomDto> CreateAsync(CreateRoomRequest request);
    Task<RoomDto?> UpdateAsync(Guid id, UpdateRoomRequest request);
    Task<bool> DeleteAsync(Guid id);
}
