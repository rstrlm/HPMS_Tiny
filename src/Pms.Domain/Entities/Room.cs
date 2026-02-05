using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class Room : BaseEntity
{
    public string RoomNumber { get; set; } = string.Empty;
    public Guid RoomTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public RoomStatus CurrentStatus { get; set; } = RoomStatus.Available;

    public RoomType? RoomType { get; set; }
    public ICollection<RoomStateBlock> StateBlocks { get; set; } = new List<RoomStateBlock>();
    public ICollection<RoomAssignment> Assignments { get; set; } = new List<RoomAssignment>();
    public ICollection<ReservationHold> Holds { get; set; } = new List<ReservationHold>();
    public ICollection<CleaningTask> CleaningTasks { get; set; } = new List<CleaningTask>();
}
