using Microsoft.EntityFrameworkCore;
using Pms.Application.Interfaces;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class TreatmentAvailabilityService : ITreatmentAvailabilityService
{
    private readonly PmsDbContext _context;

    public TreatmentAvailabilityService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsRoomAvailableAsync(Guid treatmentRoomId, DateTime startUtc, DateTime endUtc, int seatsNeeded = 1, Guid? excludeAppointmentId = null)
    {
        var room = await _context.TreatmentRooms.FindAsync(treatmentRoomId);
        if (room is null || !room.IsActive)
            return false;

        // Get sum of SeatsUsed for overlapping appointments
        var overlappingSeats = await _context.TreatmentAppointments
            .Where(ta => ta.TreatmentRoomId == treatmentRoomId)
            .Where(ta => excludeAppointmentId == null || ta.Id != excludeAppointmentId)
            .Where(ta => ta.Status != AppointmentStatus.Cancelled)
            .Where(ta => ta.StartAtUtc < endUtc && ta.EndAtUtc > startUtc)
            .SumAsync(ta => ta.SeatsUsed);

        return overlappingSeats + seatsNeeded <= room.Capacity;
    }

    public async Task<bool> IsTherapistAvailableAsync(Guid therapistStaffId, DateTime startUtc, DateTime endUtc, Guid? excludeAppointmentId = null)
    {
        var staff = await _context.StaffProfiles.FindAsync(therapistStaffId);
        if (staff is null || !staff.IsActive)
            return false;

        // Therapist capacity is 1 - any overlapping appointment blocks
        var hasOverlapping = await _context.TreatmentAppointments
            .Where(ta => ta.TherapistStaffId == therapistStaffId)
            .Where(ta => excludeAppointmentId == null || ta.Id != excludeAppointmentId)
            .Where(ta => ta.Status != AppointmentStatus.Cancelled)
            .Where(ta => ta.StartAtUtc < endUtc && ta.EndAtUtc > startUtc)
            .AnyAsync();

        return !hasOverlapping;
    }

    public async Task<int> GetRoomOccupancyAsync(Guid treatmentRoomId, DateTime atTimeUtc)
    {
        return await _context.TreatmentAppointments
            .Where(ta => ta.TreatmentRoomId == treatmentRoomId)
            .Where(ta => ta.Status != AppointmentStatus.Cancelled)
            .Where(ta => ta.StartAtUtc <= atTimeUtc && ta.EndAtUtc > atTimeUtc)
            .SumAsync(ta => ta.SeatsUsed);
    }

    public async Task<IEnumerable<TimeSlot>> GetAvailableTimeSlotsAsync(
        Guid treatmentRoomId,
        DateOnly date,
        int durationMinutes,
        int seatsNeeded = 1,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null)
    {
        var room = await _context.TreatmentRooms.FindAsync(treatmentRoomId);
        if (room is null || !room.IsActive)
            return Enumerable.Empty<TimeSlot>();

        // Default business hours: 8:00 - 20:00
        var dayStart = startTime ?? new TimeOnly(8, 0);
        var dayEnd = endTime ?? new TimeOnly(20, 0);

        var startDateTimeUtc = date.ToDateTime(dayStart, DateTimeKind.Utc);
        var endDateTimeUtc = date.ToDateTime(dayEnd, DateTimeKind.Utc);

        // Get all appointments for this room on this day
        var appointments = await _context.TreatmentAppointments
            .Where(ta => ta.TreatmentRoomId == treatmentRoomId)
            .Where(ta => ta.Status != AppointmentStatus.Cancelled)
            .Where(ta => ta.StartAtUtc < endDateTimeUtc && ta.EndAtUtc > startDateTimeUtc)
            .OrderBy(ta => ta.StartAtUtc)
            .Select(ta => new { ta.StartAtUtc, ta.EndAtUtc, ta.SeatsUsed })
            .ToListAsync();

        var availableSlots = new List<TimeSlot>();
        var slotInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

        for (var slotStart = startDateTimeUtc; slotStart.Add(TimeSpan.FromMinutes(durationMinutes)) <= endDateTimeUtc; slotStart = slotStart.Add(slotInterval))
        {
            var slotEnd = slotStart.Add(TimeSpan.FromMinutes(durationMinutes));

            // Calculate max concurrent occupancy during this slot
            var maxOccupancy = 0;
            foreach (var apt in appointments)
            {
                if (apt.StartAtUtc < slotEnd && apt.EndAtUtc > slotStart)
                {
                    // This appointment overlaps with the slot - need to check concurrent occupancy
                    var concurrentSeats = appointments
                        .Where(a => a.StartAtUtc < slotEnd && a.EndAtUtc > slotStart)
                        .Sum(a => a.SeatsUsed);
                    maxOccupancy = Math.Max(maxOccupancy, concurrentSeats);
                }
            }

            // Simpler approach: sum all overlapping seats
            var overlappingSeats = appointments
                .Where(a => a.StartAtUtc < slotEnd && a.EndAtUtc > slotStart)
                .Sum(a => a.SeatsUsed);

            var availableCapacity = room.Capacity - overlappingSeats;

            if (availableCapacity >= seatsNeeded)
            {
                availableSlots.Add(new TimeSlot(slotStart, slotEnd, availableCapacity));
            }
        }

        return availableSlots;
    }

    public async Task<IEnumerable<Guid>> GetAvailableTherapistsAsync(DateTime startUtc, DateTime endUtc)
    {
        // Get all active therapists
        var allTherapists = await _context.StaffProfiles
            .Where(sp => sp.IsActive)
            .Select(sp => sp.Id)
            .ToListAsync();

        // Get therapists with overlapping appointments
        var busyTherapists = await _context.TreatmentAppointments
            .Where(ta => ta.TherapistStaffId != null)
            .Where(ta => ta.Status != AppointmentStatus.Cancelled)
            .Where(ta => ta.StartAtUtc < endUtc && ta.EndAtUtc > startUtc)
            .Select(ta => ta.TherapistStaffId!.Value)
            .Distinct()
            .ToListAsync();

        return allTherapists.Except(busyTherapists);
    }
}
