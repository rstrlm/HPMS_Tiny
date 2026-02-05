using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid FolioId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ProviderReference { get; set; }
    public Guid? ProcessedByStaffId { get; set; }

    public Folio? Folio { get; set; }
    public StaffProfile? ProcessedByStaff { get; set; }
}
