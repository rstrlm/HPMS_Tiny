namespace Pms.Application.DTOs;

public record RoomTypeDto(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    decimal BasePrice,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateRoomTypeRequest(
    string Name,
    string? Description,
    int Capacity,
    decimal BasePrice);

public record UpdateRoomTypeRequest(
    string Name,
    string? Description,
    int Capacity,
    decimal BasePrice);
