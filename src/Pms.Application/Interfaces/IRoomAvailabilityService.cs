namespace Pms.Application.Interfaces;

public interface IRoomAvailabilityService
{
    /// <summary>
    /// Checks if a specific room is available for the given date range.
    /// A room is available if:
    /// - No overlapping RoomAssignment exists
    /// - No overlapping RoomStateBlock (Maintenance/OutOfService) exists
    /// - No active (non-expired) ReservationHold exists
    /// - Room is active
    /// </summary>
    Task<bool> IsRoomAvailableAsync(Guid roomId, DateOnly fromDate, DateOnly toDate, Guid? excludeReservationId = null);

    /// <summary>
    /// Gets all available rooms of a specific type for the given date range.
    /// </summary>
    Task<IEnumerable<Guid>> GetAvailableRoomIdsAsync(Guid? roomTypeId, DateOnly fromDate, DateOnly toDate, int roomsNeeded = 1);

    /// <summary>
    /// Gets detailed availability info for rooms in a date range.
    /// </summary>
    Task<IEnumerable<RoomAvailabilityInfo>> GetRoomAvailabilityAsync(DateOnly fromDate, DateOnly toDate, Guid? roomTypeId = null);

    /// <summary>
    /// Places a temporary hold on a room to prevent race conditions during booking.
    /// </summary>
    Task<Guid> PlaceHoldAsync(Guid roomId, DateOnly fromDate, DateOnly toDate, Guid? staffId = null, string? sessionId = null, int holdMinutes = 10);

    /// <summary>
    /// Releases a hold on a room.
    /// </summary>
    Task<bool> ReleaseHoldAsync(Guid holdId);

    /// <summary>
    /// Cleans up expired holds.
    /// </summary>
    Task<int> CleanupExpiredHoldsAsync();
}

public record RoomAvailabilityInfo(
    Guid RoomId,
    string RoomNumber,
    Guid RoomTypeId,
    string RoomTypeName,
    bool IsAvailable,
    string? BlockedReason);
