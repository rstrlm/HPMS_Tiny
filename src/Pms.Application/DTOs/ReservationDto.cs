using Pms.Domain.Enums;

namespace Pms.Application.DTOs;

public record ReservationDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    ReservationStatus Status,
    string? Notes,
    int NumberOfGuests,
    IEnumerable<RoomAssignmentDto> RoomAssignments,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record RoomAssignmentDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    string? RoomTypeName,
    DateOnly FromDate,
    DateOnly ToDate);

public record CreateReservationRequest(
    Guid? CustomerId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int NumberOfGuests,
    string? Notes,
    IEnumerable<CreateRoomAssignmentRequest> RoomAssignments,
    CreateCustomerRequest? NewCustomer = null,
    IEnumerable<CreateReservationAppointmentRequest>? Appointments = null);

/// <summary>
/// Simplified appointment request for reservation creation.
/// </summary>
public record CreateReservationAppointmentRequest(
    Guid TreatmentTypeId,
    Guid TreatmentRoomId,
    Guid? TherapistStaffId,
    DateTime StartAtUtc,
    int SeatsUsed = 1,
    string? Notes = null);

public record CreateRoomAssignmentRequest(
    Guid RoomId,
    DateOnly FromDate,
    DateOnly ToDate);

public record UpdateReservationRequest(
    DateOnly? CheckInDate,
    DateOnly? CheckOutDate,
    int? NumberOfGuests,
    string? Notes);

public record ChangeReservationStatusRequest(
    ReservationStatus Status);

public record ReservationAvailabilityRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? RoomTypeId,
    int RoomsNeeded = 1);

public record PlaceHoldRequest(
    Guid RoomId,
    DateOnly FromDate,
    DateOnly ToDate,
    int HoldMinutes = 10);

public record HoldDto(
    Guid Id,
    Guid RoomId,
    DateOnly FromDate,
    DateOnly ToDate,
    DateTime ExpiresAtUtc);
