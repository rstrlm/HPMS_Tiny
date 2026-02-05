using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class ReservationService : IReservationService
{
    private readonly PmsDbContext _context;
    private readonly IRoomAvailabilityService _availabilityService;
    private readonly IHousekeepingService _housekeepingService;
    private readonly IAppointmentService? _appointmentService;
    private readonly IFolioService? _folioService;

    public ReservationService(
        PmsDbContext context,
        IRoomAvailabilityService availabilityService,
        IHousekeepingService housekeepingService,
        IAppointmentService? appointmentService = null,
        IFolioService? folioService = null)
    {
        _context = context;
        _availabilityService = availabilityService;
        _housekeepingService = housekeepingService;
        _appointmentService = appointmentService;
        _folioService = folioService;
    }

    public async Task<ReservationDto?> GetByIdAsync(Guid id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Customer)
            .Include(r => r.RoomAssignments)
                .ThenInclude(ra => ra.Room)
                    .ThenInclude(r => r!.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);

        return reservation is null ? null : MapToDto(reservation);
    }

    public async Task<IEnumerable<ReservationDto>> GetAllAsync(DateOnly? fromDate = null, DateOnly? toDate = null, ReservationStatus? status = null)
    {
        var query = _context.Reservations
            .Include(r => r.Customer)
            .Include(r => r.RoomAssignments)
                .ThenInclude(ra => ra.Room)
                    .ThenInclude(r => r!.RoomType)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CheckOutDate > fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CheckInDate < toDate.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var reservations = await query
            .OrderBy(r => r.CheckInDate)
            .ToListAsync();

        return reservations.Select(MapToDto);
    }

    public async Task<IEnumerable<ReservationDto>> GetByCustomerAsync(Guid customerId)
    {
        var reservations = await _context.Reservations
            .Include(r => r.Customer)
            .Include(r => r.RoomAssignments)
                .ThenInclude(ra => ra.Room)
                    .ThenInclude(r => r!.RoomType)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CheckInDate)
            .ToListAsync();

        return reservations.Select(MapToDto);
    }

    public async Task<ReservationDto> CreateAsync(CreateReservationRequest request)
    {
        // Validate date range
        if (request.CheckOutDate <= request.CheckInDate)
        {
            throw new InvalidOperationException("Check-out date must be after check-in date.");
        }

        // Use transaction for atomicity
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            Guid customerId;

            // Handle inline customer creation
            if (request.NewCustomer is not null)
            {
                var newCustomer = new Customer
                {
                    Name = request.NewCustomer.Name,
                    Phone = request.NewCustomer.Phone,
                    Email = request.NewCustomer.Email,
                    Address = request.NewCustomer.Address,
                    Notes = request.NewCustomer.Notes
                };
                _context.Customers.Add(newCustomer);
                await _context.SaveChangesAsync();
                customerId = newCustomer.Id;
            }
            else if (request.CustomerId.HasValue)
            {
                // Validate customer exists
                var customer = await _context.Customers.FindAsync(request.CustomerId.Value);
                if (customer is null)
                {
                    throw new InvalidOperationException("Customer not found.");
                }
                customerId = request.CustomerId.Value;
            }
            else
            {
                throw new InvalidOperationException("Either CustomerId or NewCustomer must be provided.");
            }

            // Validate all rooms are available
            foreach (var assignment in request.RoomAssignments)
            {
                var isAvailable = await _availabilityService.IsRoomAvailableAsync(
                    assignment.RoomId,
                    assignment.FromDate,
                    assignment.ToDate);

                if (!isAvailable)
                {
                    var room = await _context.Rooms.FindAsync(assignment.RoomId);
                    throw new InvalidOperationException($"Room {room?.RoomNumber ?? assignment.RoomId.ToString()} is not available for the requested dates.");
                }
            }

            var reservation = new Reservation
            {
                CustomerId = customerId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                NumberOfGuests = request.NumberOfGuests,
                Notes = request.Notes,
                Status = ReservationStatus.Confirmed
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Create room assignments
            foreach (var assignmentRequest in request.RoomAssignments)
            {
                var assignment = new RoomAssignment
                {
                    ReservationId = reservation.Id,
                    RoomId = assignmentRequest.RoomId,
                    FromDate = assignmentRequest.FromDate,
                    ToDate = assignmentRequest.ToDate
                };
                _context.RoomAssignments.Add(assignment);
            }

            await _context.SaveChangesAsync();

            // Create treatment appointments if provided
            if (request.Appointments?.Any() == true && _appointmentService is not null)
            {
                foreach (var appt in request.Appointments)
                {
                    await _appointmentService.CreateAsync(new CreateAppointmentRequest(
                        CustomerId: customerId,
                        ReservationId: reservation.Id,
                        TreatmentTypeId: appt.TreatmentTypeId,
                        TreatmentRoomId: appt.TreatmentRoomId,
                        TherapistStaffId: appt.TherapistStaffId,
                        StartAtUtc: appt.StartAtUtc,
                        SeatsUsed: appt.SeatsUsed,
                        Notes: appt.Notes
                    ));
                }
            }

            // Release any holds on the booked rooms (by the same session if applicable)
            var roomIds = request.RoomAssignments.Select(a => a.RoomId).ToList();
            var holdsToRelease = await _context.ReservationHolds
                .Where(h => roomIds.Contains(h.RoomId))
                .ToListAsync();
            _context.ReservationHolds.RemoveRange(holdsToRelease);
            await _context.SaveChangesAsync();

            // Create folio with charges
            if (_folioService is not null)
            {
                var folio = await _folioService.CreateAsync(new CreateFolioRequest(
                    CustomerId: customerId,
                    ReservationId: reservation.Id));

                // Add room charges
                var numberOfNights = (request.CheckOutDate.ToDateTime(TimeOnly.MinValue) -
                                      request.CheckInDate.ToDateTime(TimeOnly.MinValue)).Days;

                foreach (var assignmentRequest in request.RoomAssignments)
                {
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.Id == assignmentRequest.RoomId);

                    if (room?.RoomType is not null)
                    {
                        await _folioService.AddChargeAsync(folio.Id, new CreateChargeRequest(
                            Type: ChargeType.RoomNight,
                            Description: $"Room {room.RoomNumber} ({room.RoomType.Name}) - {numberOfNights} night(s)",
                            Quantity: numberOfNights,
                            UnitPrice: room.RoomType.BasePrice,
                            VatRate: 0.10m // 10% VAT for accommodation
                        ));
                    }
                }

                // Add treatment charges
                if (request.Appointments?.Any() == true)
                {
                    foreach (var appt in request.Appointments)
                    {
                        var treatmentType = await _context.TreatmentTypes.FindAsync(appt.TreatmentTypeId);
                        if (treatmentType is not null)
                        {
                            await _folioService.AddChargeAsync(folio.Id, new CreateChargeRequest(
                                Type: ChargeType.Treatment,
                                Description: $"Treatment: {treatmentType.Name}",
                                Quantity: appt.SeatsUsed,
                                UnitPrice: treatmentType.BasePrice,
                                VatRate: 0.24m // 24% VAT for services
                            ));
                        }
                    }
                }
            }

            await transaction.CommitAsync();

            // Reload with all includes
            return (await GetByIdAsync(reservation.Id))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ReservationDto?> UpdateAsync(Guid id, UpdateReservationRequest request)
    {
        var reservation = await _context.Reservations
            .Include(r => r.RoomAssignments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
            return null;

        // Validate new dates if provided
        var newCheckIn = request.CheckInDate ?? reservation.CheckInDate;
        var newCheckOut = request.CheckOutDate ?? reservation.CheckOutDate;

        if (newCheckOut <= newCheckIn)
        {
            throw new InvalidOperationException("Check-out date must be after check-in date.");
        }

        // If dates changed, validate availability for existing assignments
        if (request.CheckInDate.HasValue || request.CheckOutDate.HasValue)
        {
            foreach (var assignment in reservation.RoomAssignments)
            {
                var isAvailable = await _availabilityService.IsRoomAvailableAsync(
                    assignment.RoomId,
                    newCheckIn,
                    newCheckOut,
                    excludeReservationId: reservation.Id);

                if (!isAvailable)
                {
                    throw new InvalidOperationException($"Room assignment conflicts with the new dates.");
                }
            }

            // Update assignment dates to match
            foreach (var assignment in reservation.RoomAssignments)
            {
                assignment.FromDate = newCheckIn;
                assignment.ToDate = newCheckOut;
            }
        }

        if (request.CheckInDate.HasValue)
            reservation.CheckInDate = request.CheckInDate.Value;

        if (request.CheckOutDate.HasValue)
            reservation.CheckOutDate = request.CheckOutDate.Value;

        if (request.NumberOfGuests.HasValue)
            reservation.NumberOfGuests = request.NumberOfGuests.Value;

        if (request.Notes is not null)
            reservation.Notes = request.Notes;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<ReservationDto?> ChangeStatusAsync(Guid id, ReservationStatus newStatus)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation is null)
            return null;

        reservation.Status = newStatus;

        // Handle status-specific side effects
        if (newStatus == ReservationStatus.CheckedIn)
        {
            // Update room statuses to Occupied
            var assignments = await _context.RoomAssignments
                .Include(ra => ra.Room)
                .Where(ra => ra.ReservationId == id)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                if (assignment.Room is not null)
                {
                    assignment.Room.CurrentStatus = RoomStatus.Occupied;
                }
            }
        }
        else if (newStatus == ReservationStatus.CheckedOut)
        {
            // Update room statuses to NeedsCleaning and create cleaning tasks
            var assignments = await _context.RoomAssignments
                .Include(ra => ra.Room)
                .Where(ra => ra.ReservationId == id)
                .ToListAsync();

            foreach (var assignment in assignments)
            {
                if (assignment.Room is not null)
                {
                    assignment.Room.CurrentStatus = RoomStatus.NeedsCleaning;

                    // Create checkout cleaning task for this room
                    await _housekeepingService.CreateAsync(new CreateCleaningTaskRequest(
                        assignment.RoomId,
                        DateOnly.FromDateTime(DateTime.UtcNow),
                        CleaningTaskType.Checkout,
                        null,
                        $"Checkout cleaning for reservation {reservation.Id}"
                    ));
                }
            }
        }
        else if (newStatus == ReservationStatus.Cancelled)
        {
            // Cancel the folio when reservation is cancelled
            if (_folioService is not null)
            {
                var folio = await _folioService.GetByReservationAsync(id);
                if (folio is not null)
                {
                    await _folioService.CancelFolioAsync(folio.Id);
                }
            }
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<RoomAssignmentDto?> AddRoomAssignmentAsync(Guid reservationId, CreateRoomAssignmentRequest request)
    {
        var reservation = await _context.Reservations.FindAsync(reservationId);
        if (reservation is null)
            return null;

        // Validate availability
        var isAvailable = await _availabilityService.IsRoomAvailableAsync(
            request.RoomId,
            request.FromDate,
            request.ToDate,
            excludeReservationId: reservationId);

        if (!isAvailable)
        {
            throw new InvalidOperationException("Room is not available for the requested dates.");
        }

        var assignment = new RoomAssignment
        {
            ReservationId = reservationId,
            RoomId = request.RoomId,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        };

        _context.RoomAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        // Load related data
        await _context.Entry(assignment).Reference(a => a.Room).LoadAsync();
        if (assignment.Room is not null)
        {
            await _context.Entry(assignment.Room).Reference(r => r.RoomType).LoadAsync();
        }

        return MapAssignmentToDto(assignment);
    }

    public async Task<bool> RemoveRoomAssignmentAsync(Guid assignmentId)
    {
        var assignment = await _context.RoomAssignments.FindAsync(assignmentId);
        if (assignment is null)
            return false;

        _context.RoomAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        return true;
    }

    private static ReservationDto MapToDto(Reservation r) => new(
        r.Id,
        r.CustomerId,
        r.Customer?.Name ?? "Unknown",
        r.CheckInDate,
        r.CheckOutDate,
        r.Status,
        r.Notes,
        r.NumberOfGuests,
        r.RoomAssignments.Select(MapAssignmentToDto),
        r.CreatedAtUtc,
        r.UpdatedAtUtc);

    private static RoomAssignmentDto MapAssignmentToDto(RoomAssignment ra) => new(
        ra.Id,
        ra.RoomId,
        ra.Room?.RoomNumber ?? "Unknown",
        ra.Room?.RoomType?.Name,
        ra.FromDate,
        ra.ToDate);
}
