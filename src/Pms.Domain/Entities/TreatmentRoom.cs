using Pms.Domain.Common;

namespace Pms.Domain.Entities;

public class TreatmentRoom : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Maximum number of concurrent customers/appointments.
    /// For a sauna this might be 10, for a massage room it's 1.
    /// </summary>
    public int Capacity { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public ICollection<TreatmentAppointment> Appointments { get; set; } = new List<TreatmentAppointment>();
}
