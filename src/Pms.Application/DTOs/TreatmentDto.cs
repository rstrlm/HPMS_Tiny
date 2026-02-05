using Pms.Domain.Enums;

namespace Pms.Application.DTOs;

// Treatment Type DTOs
public record TreatmentTypeDto(
    Guid Id,
    string Name,
    string? Description,
    int DurationMinutes,
    int BufferMinutes,
    decimal BasePrice,
    bool IsActive,
    bool RequiresTherapist);

public record CreateTreatmentTypeRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    int BufferMinutes,
    decimal BasePrice,
    bool RequiresTherapist = true);

public record UpdateTreatmentTypeRequest(
    string Name,
    string? Description,
    int DurationMinutes,
    int BufferMinutes,
    decimal BasePrice,
    bool IsActive,
    bool RequiresTherapist);

// Treatment Room DTOs
public record TreatmentRoomDto(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    bool IsActive);

public record CreateTreatmentRoomRequest(
    string Name,
    string? Description,
    int Capacity);

public record UpdateTreatmentRoomRequest(
    string Name,
    string? Description,
    int Capacity,
    bool IsActive);

// Appointment DTOs
public record AppointmentDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? ReservationId,
    Guid TreatmentTypeId,
    string TreatmentTypeName,
    Guid TreatmentRoomId,
    string TreatmentRoomName,
    Guid? TherapistStaffId,
    string? TherapistName,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    int SeatsUsed,
    AppointmentStatus Status,
    string? Notes,
    DateTime CreatedAtUtc);

public record CreateAppointmentRequest(
    Guid CustomerId,
    Guid? ReservationId,
    Guid TreatmentTypeId,
    Guid TreatmentRoomId,
    Guid? TherapistStaffId,
    DateTime StartAtUtc,
    int SeatsUsed = 1,
    string? Notes = null);

public record UpdateAppointmentRequest(
    DateTime? StartAtUtc,
    Guid? TreatmentRoomId,
    Guid? TherapistStaffId,
    int? SeatsUsed,
    string? Notes);

public record ChangeAppointmentStatusRequest(
    AppointmentStatus Status);

// Availability DTOs
public record TreatmentRoomAvailabilityRequest(
    Guid TreatmentRoomId,
    DateOnly Date,
    int DurationMinutes,
    int SeatsNeeded = 1);
