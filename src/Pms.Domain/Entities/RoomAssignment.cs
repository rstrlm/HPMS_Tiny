using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class RoomAssignment : BaseEntity
{
    public Guid ReservationId { get; set; }
    public Guid RoomId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; } // Exclusive (checkout date)

    public Reservation? Reservation { get; set; }
    public Room? Room { get; set; }
}
