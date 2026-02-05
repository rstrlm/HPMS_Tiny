using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Reservation : BaseEntity
{
    public Guid CustomerId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Draft;
    public string? Notes { get; set; }
    public int NumberOfGuests { get; set; } = 1;

    public Customer? Customer { get; set; }
    public ICollection<RoomAssignment> RoomAssignments { get; set; } = new List<RoomAssignment>();
    public ICollection<Folio> Folios { get; set; } = new List<Folio>();
}
