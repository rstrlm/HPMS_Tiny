using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class AppointmentService : IAppointmentService
{
    private readonly PmsDbContext _context;
    private readonly ITreatmentAvailabilityService _availabilityService;

    public AppointmentService(PmsDbContext context, ITreatmentAvailabilityService availabilityService)
    {
        _context = context;
        _availabilityService = availabilityService;
    }

    public async Task<AppointmentDto?> GetByIdAsync(Guid id)
    {
        var appointment = await _context.TreatmentAppointments
            .Include(ta => ta.Customer)
            .Include(ta => ta.TreatmentType)
            .Include(ta => ta.TreatmentRoom)
            .Include(ta => ta.TherapistStaff)
            .FirstOrDefaultAsync(ta => ta.Id == id);

        return appointment is null ? null : MapToDto(appointment);
    }

    public async Task<IEnumerable<AppointmentDto>> GetAllAsync(DateTime? from = null, DateTime? to = null, Guid? therapistId = null)
    {
        var query = _context.TreatmentAppointments
            .Include(ta => ta.Customer)
            .Include(ta => ta.TreatmentType)
            .Include(ta => ta.TreatmentRoom)
            .Include(ta => ta.TherapistStaff)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(ta => ta.EndAtUtc > from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(ta => ta.StartAtUtc < to.Value);
        }

        if (therapistId.HasValue)
        {
            query = query.Where(ta => ta.TherapistStaffId == therapistId.Value);
        }

        var appointments = await query
            .OrderBy(ta => ta.StartAtUtc)
            .ToListAsync();

        return appointments.Select(MapToDto);
    }

    public async Task<IEnumerable<AppointmentDto>> GetByCustomerAsync(Guid customerId)
    {
        var appointments = await _context.TreatmentAppointments
            .Include(ta => ta.Customer)
            .Include(ta => ta.TreatmentType)
            .Include(ta => ta.TreatmentRoom)
            .Include(ta => ta.TherapistStaff)
            .Where(ta => ta.CustomerId == customerId)
            .OrderByDescending(ta => ta.StartAtUtc)
            .ToListAsync();

        return appointments.Select(MapToDto);
    }

    public async Task<IEnumerable<AppointmentDto>> GetByReservationAsync(Guid reservationId)
    {
        var appointments = await _context.TreatmentAppointments
            .Include(ta => ta.Customer)
            .Include(ta => ta.TreatmentType)
            .Include(ta => ta.TreatmentRoom)
            .Include(ta => ta.TherapistStaff)
            .Where(ta => ta.ReservationId == reservationId)
            .OrderBy(ta => ta.StartAtUtc)
            .ToListAsync();

        return appointments.Select(MapToDto);
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request)
    {
        // Validate customer exists
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer not found.");
        }

        // Validate treatment type exists
        var treatmentType = await _context.TreatmentTypes.FindAsync(request.TreatmentTypeId);
        if (treatmentType is null || !treatmentType.IsActive)
        {
            throw new InvalidOperationException("Treatment type not found or inactive.");
        }

        // Calculate end time based on treatment duration
        var endAtUtc = request.StartAtUtc.AddMinutes(treatmentType.DurationMinutes);

        // Validate room capacity
        var isRoomAvailable = await _availabilityService.IsRoomAvailableAsync(
            request.TreatmentRoomId,
            request.StartAtUtc,
            endAtUtc,
            request.SeatsUsed);

        if (!isRoomAvailable)
        {
            throw new InvalidOperationException("Treatment room does not have enough capacity for the requested time slot.");
        }

        // Validate therapist availability if required and assigned
        if (treatmentType.RequiresTherapist)
        {
            if (!request.TherapistStaffId.HasValue)
            {
                throw new InvalidOperationException("This treatment requires a therapist.");
            }

            var isTherapistAvailable = await _availabilityService.IsTherapistAvailableAsync(
                request.TherapistStaffId.Value,
                request.StartAtUtc,
                endAtUtc);

            if (!isTherapistAvailable)
            {
                throw new InvalidOperationException("Therapist is not available for the requested time slot.");
            }
        }
        else if (request.TherapistStaffId.HasValue)
        {
            // Therapist assigned but not required - still check availability
            var isTherapistAvailable = await _availabilityService.IsTherapistAvailableAsync(
                request.TherapistStaffId.Value,
                request.StartAtUtc,
                endAtUtc);

            if (!isTherapistAvailable)
            {
                throw new InvalidOperationException("Therapist is not available for the requested time slot.");
            }
        }

        var appointment = new TreatmentAppointment
        {
            CustomerId = request.CustomerId,
            ReservationId = request.ReservationId,
            TreatmentTypeId = request.TreatmentTypeId,
            TreatmentRoomId = request.TreatmentRoomId,
            TherapistStaffId = request.TherapistStaffId,
            StartAtUtc = request.StartAtUtc,
            EndAtUtc = endAtUtc,
            SeatsUsed = request.SeatsUsed,
            Status = AppointmentStatus.Confirmed,
            Notes = request.Notes
        };

        _context.TreatmentAppointments.Add(appointment);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(appointment.Id))!;
    }

    public async Task<AppointmentDto?> UpdateAsync(Guid id, UpdateAppointmentRequest request)
    {
        var appointment = await _context.TreatmentAppointments
            .Include(ta => ta.TreatmentType)
            .FirstOrDefaultAsync(ta => ta.Id == id);

        if (appointment is null)
            return null;

        var startAtUtc = request.StartAtUtc ?? appointment.StartAtUtc;
        var durationMinutes = appointment.TreatmentType!.DurationMinutes;
        var endAtUtc = startAtUtc.AddMinutes(durationMinutes);
        var roomId = request.TreatmentRoomId ?? appointment.TreatmentRoomId;
        var seatsUsed = request.SeatsUsed ?? appointment.SeatsUsed;
        var therapistId = request.TherapistStaffId ?? appointment.TherapistStaffId;

        // Validate room capacity if room or time changed
        if (request.StartAtUtc.HasValue || request.TreatmentRoomId.HasValue || request.SeatsUsed.HasValue)
        {
            var isRoomAvailable = await _availabilityService.IsRoomAvailableAsync(
                roomId,
                startAtUtc,
                endAtUtc,
                seatsUsed,
                excludeAppointmentId: id);

            if (!isRoomAvailable)
            {
                throw new InvalidOperationException("Treatment room does not have enough capacity for the requested time slot.");
            }
        }

        // Validate therapist availability if therapist or time changed
        if (therapistId.HasValue && (request.StartAtUtc.HasValue || request.TherapistStaffId.HasValue))
        {
            var isTherapistAvailable = await _availabilityService.IsTherapistAvailableAsync(
                therapistId.Value,
                startAtUtc,
                endAtUtc,
                excludeAppointmentId: id);

            if (!isTherapistAvailable)
            {
                throw new InvalidOperationException("Therapist is not available for the requested time slot.");
            }
        }

        if (request.StartAtUtc.HasValue)
        {
            appointment.StartAtUtc = startAtUtc;
            appointment.EndAtUtc = endAtUtc;
        }

        if (request.TreatmentRoomId.HasValue)
            appointment.TreatmentRoomId = request.TreatmentRoomId.Value;

        if (request.TherapistStaffId.HasValue)
            appointment.TherapistStaffId = request.TherapistStaffId;

        if (request.SeatsUsed.HasValue)
            appointment.SeatsUsed = request.SeatsUsed.Value;

        if (request.Notes is not null)
            appointment.Notes = request.Notes;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<AppointmentDto?> ChangeStatusAsync(Guid id, AppointmentStatus newStatus)
    {
        var appointment = await _context.TreatmentAppointments.FindAsync(id);
        if (appointment is null)
            return null;

        appointment.Status = newStatus;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var appointment = await _context.TreatmentAppointments.FindAsync(id);
        if (appointment is null)
            return false;

        _context.TreatmentAppointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return true;
    }

    private static AppointmentDto MapToDto(TreatmentAppointment ta) => new(
        ta.Id,
        ta.CustomerId,
        ta.Customer?.Name ?? "Unknown",
        ta.ReservationId,
        ta.TreatmentTypeId,
        ta.TreatmentType?.Name ?? "Unknown",
        ta.TreatmentRoomId,
        ta.TreatmentRoom?.Name ?? "Unknown",
        ta.TherapistStaffId,
        ta.TherapistStaff?.DisplayName,
        ta.StartAtUtc,
        ta.EndAtUtc,
        ta.SeatsUsed,
        ta.Status,
        ta.Notes,
        ta.CreatedAtUtc);
}
