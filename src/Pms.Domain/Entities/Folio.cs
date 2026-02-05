using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Folio : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid? ReservationId { get; set; }
    public FolioStatus Status { get; set; } = FolioStatus.Open;

    public Customer? Customer { get; set; }
    public Reservation? Reservation { get; set; }
    public ICollection<Charge> Charges { get; set; } = new List<Charge>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
