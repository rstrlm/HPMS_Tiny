using Pms.Domain.Common;
using Pms.Domain.Enums;

namespace Pms.Domain.Entities;

public class TreatmentAppointment : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid TreatmentTypeId { get; set; }
    public Guid TreatmentRoomId { get; set; }
    public Guid? TherapistStaffId { get; set; }

    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }

    /// <summary>
    /// Number of seats/capacity units used by this appointment.
    /// Default is 1, but could be more for group bookings.
    /// </summary>
    public int SeatsUsed { get; set; } = 1;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Draft;
    public string? Notes { get; set; }

    public Customer? Customer { get; set; }
    public Reservation? Reservation { get; set; }
    public TreatmentType? TreatmentType { get; set; }
    public TreatmentRoom? TreatmentRoom { get; set; }
    public StaffProfile? TherapistStaff { get; set; }
}
