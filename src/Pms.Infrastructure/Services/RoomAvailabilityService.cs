using Microsoft.EntityFrameworkCore;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class RoomAvailabilityService : IRoomAvailabilityService
{
    private readonly PmsDbContext _context;

    public RoomAvailabilityService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsRoomAvailableAsync(Guid roomId, DateOnly fromDate, DateOnly toDate, Guid? excludeReservationId = null)
    {
        // Check if room exists and is active
        var room = await _context.Rooms.FindAsync(roomId);
        if (room is null || !room.IsActive)
            return false;

        // Check for overlapping assignments (excluding the specified reservation if updating)
        var hasOverlappingAssignment = await _context.RoomAssignments
            .Where(ra => ra.RoomId == roomId)
            .Where(ra => excludeReservationId == null || ra.ReservationId != excludeReservationId)
            .Where(ra => ra.Reservation!.Status != ReservationStatus.Cancelled)
            .Where(ra => ra.FromDate < toDate && ra.ToDate > fromDate)
            .AnyAsync();

        if (hasOverlappingAssignment)
            return false;

        // Check for overlapping state blocks (Maintenance/OutOfService)
        var fromDateUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateUtc = toDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var hasOverlappingBlock = await _context.RoomStateBlocks
            .Where(rsb => rsb.RoomId == roomId)
            .Where(rsb => rsb.StartAtUtc < toDateUtc && rsb.EndAtUtc > fromDateUtc)
            .AnyAsync();

        if (hasOverlappingBlock)
            return false;

        // Check for active (non-expired) holds
        var now = DateTime.UtcNow;
        var hasActiveHold = await _context.ReservationHolds
            .Where(rh => rh.RoomId == roomId)
            .Where(rh => rh.ExpiresAtUtc > now)
            .Where(rh => rh.FromDate < toDate && rh.ToDate > fromDate)
            .AnyAsync();

        if (hasActiveHold)
            return false;

        return true;
    }

    public async Task<IEnumerable<Guid>> GetAvailableRoomIdsAsync(Guid? roomTypeId, DateOnly fromDate, DateOnly toDate, int roomsNeeded = 1)
    {
        var query = _context.Rooms
            .Where(r => r.IsActive);

        if (roomTypeId.HasValue)
        {
            query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
        }

        var candidateRooms = await query.Select(r => r.Id).ToListAsync();
        var availableRooms = new List<Guid>();

        foreach (var roomId in candidateRooms)
        {
            if (await IsRoomAvailableAsync(roomId, fromDate, toDate))
            {
                availableRooms.Add(roomId);
                if (availableRooms.Count >= roomsNeeded)
                    break;
            }
        }

        return availableRooms;
    }

    public async Task<IEnumerable<RoomAvailabilityInfo>> GetRoomAvailabilityAsync(DateOnly fromDate, DateOnly toDate, Guid? roomTypeId = null)
    {
        var query = _context.Rooms
            .Include(r => r.RoomType)
            .Where(r => r.IsActive);

        if (roomTypeId.HasValue)
        {
            query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
        }

        var rooms = await query.OrderBy(r => r.RoomNumber).ToListAsync();
        var result = new List<RoomAvailabilityInfo>();

        var fromDateUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateUtc = toDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var now = DateTime.UtcNow;

        foreach (var room in rooms)
        {
            string? blockedReason = null;

            // Check assignments
            var hasAssignment = await _context.RoomAssignments
                .Where(ra => ra.RoomId == room.Id)
                .Where(ra => ra.Reservation!.Status != ReservationStatus.Cancelled)
                .Where(ra => ra.FromDate < toDate && ra.ToDate > fromDate)
                .AnyAsync();

            if (hasAssignment)
            {
                blockedReason = "Reserved";
            }
            else
            {
                // Check state blocks
                var stateBlock = await _context.RoomStateBlocks
                    .Where(rsb => rsb.RoomId == room.Id)
                    .Where(rsb => rsb.StartAtUtc < toDateUtc && rsb.EndAtUtc > fromDateUtc)
                    .FirstOrDefaultAsync();

                if (stateBlock is not null)
                {
                    blockedReason = stateBlock.Type == RoomStateBlockType.Maintenance ? "Maintenance" : "Out of Service";
                }
                else
                {
                    // Check holds
                    var hasHold = await _context.ReservationHolds
                        .Where(rh => rh.RoomId == room.Id)
                        .Where(rh => rh.ExpiresAtUtc > now)
                        .Where(rh => rh.FromDate < toDate && rh.ToDate > fromDate)
                        .AnyAsync();

                    if (hasHold)
                    {
                        blockedReason = "On Hold";
                    }
                }
            }

            result.Add(new RoomAvailabilityInfo(
                room.Id,
                room.RoomNumber,
                room.RoomTypeId,
                room.RoomType?.Name ?? "Unknown",
                blockedReason is null,
                blockedReason));
        }

        return result;
    }

    public async Task<Guid> PlaceHoldAsync(Guid roomId, DateOnly fromDate, DateOnly toDate, Guid? staffId = null, string? sessionId = null, int holdMinutes = 10)
    {
        var hold = new ReservationHold
        {
            RoomId = roomId,
            FromDate = fromDate,
            ToDate = toDate,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(holdMinutes),
            HeldByStaffId = staffId,
            SessionId = sessionId
        };

        _context.ReservationHolds.Add(hold);
        await _context.SaveChangesAsync();

        return hold.Id;
    }

    public async Task<bool> ReleaseHoldAsync(Guid holdId)
    {
        var hold = await _context.ReservationHolds.FindAsync(holdId);
        if (hold is null)
            return false;

        _context.ReservationHolds.Remove(hold);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> CleanupExpiredHoldsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredHolds = await _context.ReservationHolds
            .Where(rh => rh.ExpiresAtUtc <= now)
            .ToListAsync();

        if (expiredHolds.Count == 0)
            return 0;

        _context.ReservationHolds.RemoveRange(expiredHolds);
        await _context.SaveChangesAsync();

        return expiredHolds.Count;
    }
}
