namespace Pms.Application.Interfaces;

public interface ITreatmentAvailabilityService
{
    /// <summary>
    /// Checks if a treatment room has capacity for the given time slot.
    /// Capacity is available if: sum of overlapping SeatsUsed + requested seats <= room Capacity
    /// </summary>
    Task<bool> IsRoomAvailableAsync(Guid treatmentRoomId, DateTime startUtc, DateTime endUtc, int seatsNeeded = 1, Guid? excludeAppointmentId = null);

    /// <summary>
    /// Checks if a therapist is available for the given time slot.
    /// Staff availability is capacity=1 by default (no overlapping appointments).
    /// </summary>
    Task<bool> IsTherapistAvailableAsync(Guid therapistStaffId, DateTime startUtc, DateTime endUtc, Guid? excludeAppointmentId = null);

    /// <summary>
    /// Gets current occupancy for a treatment room at a specific time.
    /// </summary>
    Task<int> GetRoomOccupancyAsync(Guid treatmentRoomId, DateTime atTimeUtc);

    /// <summary>
    /// Gets available time slots for a treatment room on a given date.
    /// </summary>
    Task<IEnumerable<TimeSlot>> GetAvailableTimeSlotsAsync(
        Guid treatmentRoomId,
        DateOnly date,
        int durationMinutes,
        int seatsNeeded = 1,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null);

    /// <summary>
    /// Gets available therapists for a given time slot.
    /// </summary>
    Task<IEnumerable<Guid>> GetAvailableTherapistsAsync(DateTime startUtc, DateTime endUtc);
}

public record TimeSlot(DateTime StartUtc, DateTime EndUtc, int AvailableCapacity);
