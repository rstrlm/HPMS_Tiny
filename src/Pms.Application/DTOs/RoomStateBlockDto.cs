using Pms.Domain.Enums;

namespace Pms.Application.DTOs;

public record RoomStateBlockDto(
    Guid Id,
    Guid RoomId,
    string? RoomNumber,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    RoomStateBlockType Type,
    string? Note,
    Guid? CreatedByStaffId,
    string? CreatedByStaffName,
    DateTime CreatedAtUtc);

public record CreateRoomStateBlockRequest(
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    RoomStateBlockType Type,
    string? Note);
