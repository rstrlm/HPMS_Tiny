using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class StaffProfile : BaseEntity
{
    public string KeycloakUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Skills { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CleaningTask> AssignedCleaningTasks { get; set; } = new List<CleaningTask>();
}
