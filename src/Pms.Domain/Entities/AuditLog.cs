using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? PerformedByStaffId { get; set; }
    public string? PerformedByKeycloakId { get; set; }

    public StaffProfile? PerformedByStaff { get; set; }
}
