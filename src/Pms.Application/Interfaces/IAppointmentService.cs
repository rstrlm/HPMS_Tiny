using Pms.Application.DTOs;
using Pms.Domain.Enums;

namespace Pms.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<AppointmentDto>> GetAllAsync(DateTime? from = null, DateTime? to = null, Guid? therapistId = null);
    Task<IEnumerable<AppointmentDto>> GetByCustomerAsync(Guid customerId);
    Task<IEnumerable<AppointmentDto>> GetByReservationAsync(Guid reservationId);

    /// <summary>
    /// Creates an appointment. Validates room capacity and therapist availability.
    /// </summary>
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request);

    Task<AppointmentDto?> UpdateAsync(Guid id, UpdateAppointmentRequest request);
    Task<AppointmentDto?> ChangeStatusAsync(Guid id, AppointmentStatus newStatus);
    Task<bool> DeleteAsync(Guid id);
}
