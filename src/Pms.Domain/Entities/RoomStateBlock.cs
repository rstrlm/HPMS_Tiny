using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class RoomStateBlock : BaseEntity
{
    public Guid RoomId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public RoomStateBlockType Type { get; set; }
    public string? Note { get; set; }
    public Guid? CreatedByStaffId { get; set; }

    public Room? Room { get; set; }
    public StaffProfile? CreatedByStaff { get; set; }
}
