using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Charge : BaseEntity
{
    public Guid FolioId { get; set; }
    public ChargeType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = 0.24m; // Finnish VAT 24%
    public Guid? CreatedByStaffId { get; set; }

    public Folio? Folio { get; set; }
    public StaffProfile? CreatedByStaff { get; set; }

    // Computed properties
    public decimal SubTotal => Quantity * UnitPrice;
    public decimal VatAmount => SubTotal * VatRate;
    public decimal Total => SubTotal + VatAmount;
}
