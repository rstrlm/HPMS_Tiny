using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface IRoomStateBlockService
{
    Task<IEnumerable<RoomStateBlockDto>> GetByRoomAsync(Guid roomId, DateTime? from = null, DateTime? to = null);
    Task<RoomStateBlockDto?> GetByIdAsync(Guid id);
    Task<RoomStateBlockDto> CreateAsync(Guid roomId, CreateRoomStateBlockRequest request, Guid? staffId = null);
    Task<bool> DeleteAsync(Guid id);
}
