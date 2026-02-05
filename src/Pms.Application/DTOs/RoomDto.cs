using Pms.Domain.Enums;

namespace Pms.Application.DTOs;

public record RoomDto(
    Guid Id,
    string RoomNumber,
    Guid RoomTypeId,
    string? RoomTypeName,
    bool IsActive,
    RoomStatus CurrentStatus,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateRoomRequest(
    string RoomNumber,
    Guid RoomTypeId);

public record UpdateRoomRequest(
    string RoomNumber,
    Guid RoomTypeId,
    bool IsActive,
    RoomStatus CurrentStatus);
