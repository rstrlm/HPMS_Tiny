using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid FolioId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;

    // Snapshot of folio totals at time of issue
    public decimal SubTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal GrandTotal { get; set; }

    public Guid? IssuedByStaffId { get; set; }

    public Folio? Folio { get; set; }
    public StaffProfile? IssuedByStaff { get; set; }
}
