using Pms.Domain.Common;

namespace Pms.Domain.Entities;

/// <summary>
/// Short-lived hold on a room to prevent race conditions during booking.
/// Holds expire automatically after a configured duration (default 10 minutes).
/// </summary>
public class ReservationHold : BaseEntity
{
    public Guid RoomId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public Guid? HeldByStaffId { get; set; }
    public string? SessionId { get; set; }

    public Room? Room { get; set; }
    public StaffProfile? HeldByStaff { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
}
