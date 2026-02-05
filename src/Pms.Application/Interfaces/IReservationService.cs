using Pms.Application.DTOs;
using Pms.Domain.Enums;

namespace Pms.Application.Interfaces;

public interface IReservationService
{
    Task<ReservationDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ReservationDto>> GetAllAsync(DateOnly? fromDate = null, DateOnly? toDate = null, ReservationStatus? status = null);
    Task<IEnumerable<ReservationDto>> GetByCustomerAsync(Guid customerId);

    /// <summary>
    /// Creates a reservation with room assignments. Uses transaction to ensure atomicity.
    /// Validates room availability before creating.
    /// </summary>
    Task<ReservationDto> CreateAsync(CreateReservationRequest request);

    Task<ReservationDto?> UpdateAsync(Guid id, UpdateReservationRequest request);
    Task<ReservationDto?> ChangeStatusAsync(Guid id, ReservationStatus newStatus);

    /// <summary>
    /// Adds a room assignment to an existing reservation.
    /// Validates availability before adding.
    /// </summary>
    Task<RoomAssignmentDto?> AddRoomAssignmentAsync(Guid reservationId, CreateRoomAssignmentRequest request);

    /// <summary>
    /// Removes a room assignment from a reservation.
    /// </summary>
    Task<bool> RemoveRoomAssignmentAsync(Guid assignmentId);
}
