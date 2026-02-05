using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class TreatmentType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int BufferMinutes { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresTherapist { get; set; } = true;
}
