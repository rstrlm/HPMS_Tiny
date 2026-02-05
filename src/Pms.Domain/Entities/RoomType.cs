using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class RoomType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
