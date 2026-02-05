using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class CleaningTask : BaseEntity
{
    public Guid RoomId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public CleaningTaskType TaskType { get; set; }
    public CleaningTaskStatus Status { get; set; } = CleaningTaskStatus.Pending;
    public Guid? AssignedToStaffId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }

    public Room? Room { get; set; }
    public StaffProfile? AssignedToStaff { get; set; }
}
