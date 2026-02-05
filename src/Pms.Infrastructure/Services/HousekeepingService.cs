using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class HousekeepingService : IHousekeepingService
{
    private readonly PmsDbContext _context;

    public HousekeepingService(PmsDbContext context)
    {
        _context = context;
    }

    public async Task<CleaningTaskDto?> GetByIdAsync(Guid id)
    {
        var task = await _context.CleaningTasks
            .Include(ct => ct.Room)
            .Include(ct => ct.AssignedToStaff)
            .FirstOrDefaultAsync(ct => ct.Id == id);

        return task is null ? null : MapToDto(task);
    }

    public async Task<IEnumerable<CleaningTaskDto>> GetTasksAsync(
        DateOnly date,
        CleaningTaskStatus? status = null,
        Guid? assignedToStaffId = null)
    {
        var query = _context.CleaningTasks
            .Include(ct => ct.Room)
            .Include(ct => ct.AssignedToStaff)
            .Where(ct => ct.ScheduledDate == date)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(ct => ct.Status == status.Value);
        }

        if (assignedToStaffId.HasValue)
        {
            query = query.Where(ct => ct.AssignedToStaffId == assignedToStaffId.Value);
        }

        var tasks = await query
            .OrderBy(ct => ct.Room!.RoomNumber)
            .ToListAsync();

        return tasks.Select(MapToDto);
    }

    public async Task<CleaningTaskSummaryDto> GetSummaryAsync(DateOnly date)
    {
        var tasks = await _context.CleaningTasks
            .Where(ct => ct.ScheduledDate == date)
            .ToListAsync();

        return new CleaningTaskSummaryDto(
            Date: date,
            TotalTasks: tasks.Count,
            Pending: tasks.Count(t => t.Status == CleaningTaskStatus.Pending),
            InProgress: tasks.Count(t => t.Status == CleaningTaskStatus.InProgress),
            Completed: tasks.Count(t => t.Status == CleaningTaskStatus.Completed),
            Skipped: tasks.Count(t => t.Status == CleaningTaskStatus.Skipped));
    }

    public async Task<CleaningTaskDto> CreateAsync(CreateCleaningTaskRequest request)
    {
        var room = await _context.Rooms.FindAsync(request.RoomId);
        if (room is null)
        {
            throw new InvalidOperationException("Room not found.");
        }

        if (request.AssignedToStaffId.HasValue)
        {
            var staff = await _context.StaffProfiles.FindAsync(request.AssignedToStaffId.Value);
            if (staff is null)
            {
                throw new InvalidOperationException("Staff member not found.");
            }
        }

        var task = new CleaningTask
        {
            RoomId = request.RoomId,
            ScheduledDate = request.ScheduledDate,
            TaskType = request.TaskType,
            AssignedToStaffId = request.AssignedToStaffId,
            Notes = request.Notes,
            Status = CleaningTaskStatus.Pending
        };

        _context.CleaningTasks.Add(task);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(task.Id))!;
    }

    public async Task<IEnumerable<CleaningTaskDto>> GenerateTasksForDateAsync(DateOnly date)
    {
        // Get all active rooms
        var rooms = await _context.Rooms
            .Where(r => r.IsActive)
            .ToListAsync();

        // Get existing tasks for this date to avoid duplicates
        var existingTaskRoomIds = await _context.CleaningTasks
            .Where(ct => ct.ScheduledDate == date)
            .Select(ct => ct.RoomId)
            .ToListAsync();

        // Get rooms with checkout on this date
        var checkoutRoomIds = await _context.RoomAssignments
            .Include(ra => ra.Reservation)
            .Where(ra => ra.ToDate == date &&
                         (ra.Reservation!.Status == ReservationStatus.Confirmed ||
                          ra.Reservation!.Status == ReservationStatus.CheckedIn))
            .Select(ra => ra.RoomId)
            .Distinct()
            .ToListAsync();

        // Get rooms that are currently occupied (stayover) - have assignment spanning this date but not checking out
        var stayoverRoomIds = await _context.RoomAssignments
            .Include(ra => ra.Reservation)
            .Where(ra => ra.FromDate < date &&
                         ra.ToDate > date &&
                         (ra.Reservation!.Status == ReservationStatus.Confirmed ||
                          ra.Reservation!.Status == ReservationStatus.CheckedIn))
            .Select(ra => ra.RoomId)
            .Distinct()
            .ToListAsync();

        var tasksToCreate = new List<CleaningTask>();

        // Create checkout tasks
        foreach (var roomId in checkoutRoomIds)
        {
            if (!existingTaskRoomIds.Contains(roomId))
            {
                tasksToCreate.Add(new CleaningTask
                {
                    RoomId = roomId,
                    ScheduledDate = date,
                    TaskType = CleaningTaskType.Checkout,
                    Status = CleaningTaskStatus.Pending
                });
            }
        }

        // Create stayover tasks
        foreach (var roomId in stayoverRoomIds)
        {
            if (!existingTaskRoomIds.Contains(roomId) && !checkoutRoomIds.Contains(roomId))
            {
                tasksToCreate.Add(new CleaningTask
                {
                    RoomId = roomId,
                    ScheduledDate = date,
                    TaskType = CleaningTaskType.Stayover,
                    Status = CleaningTaskStatus.Pending
                });
            }
        }

        if (tasksToCreate.Any())
        {
            _context.CleaningTasks.AddRange(tasksToCreate);
            await _context.SaveChangesAsync();
        }

        // Return all tasks for this date
        return await GetTasksAsync(date);
    }

    public async Task<CleaningTaskDto?> UpdateAsync(Guid id, UpdateCleaningTaskRequest request)
    {
        var task = await _context.CleaningTasks
            .Include(ct => ct.Room)
            .FirstOrDefaultAsync(ct => ct.Id == id);

        if (task is null)
            return null;

        if (request.AssignedToStaffId.HasValue)
        {
            var staff = await _context.StaffProfiles.FindAsync(request.AssignedToStaffId.Value);
            if (staff is null)
            {
                throw new InvalidOperationException("Staff member not found.");
            }
            task.AssignedToStaffId = request.AssignedToStaffId.Value;
        }

        if (request.Status.HasValue)
        {
            await UpdateTaskStatusAsync(task, request.Status.Value);
        }

        if (request.Notes is not null)
        {
            task.Notes = request.Notes;
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<CleaningTaskDto?> AssignAsync(Guid taskId, Guid staffId)
    {
        var task = await _context.CleaningTasks.FindAsync(taskId);
        if (task is null)
            return null;

        var staff = await _context.StaffProfiles.FindAsync(staffId);
        if (staff is null)
        {
            throw new InvalidOperationException("Staff member not found.");
        }

        task.AssignedToStaffId = staffId;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(taskId);
    }

    public async Task<CleaningTaskDto?> StartAsync(Guid taskId)
    {
        var task = await _context.CleaningTasks
            .Include(ct => ct.Room)
            .FirstOrDefaultAsync(ct => ct.Id == taskId);

        if (task is null)
            return null;

        if (task.Status != CleaningTaskStatus.Pending)
        {
            throw new InvalidOperationException("Only pending tasks can be started.");
        }

        task.Status = CleaningTaskStatus.InProgress;
        task.StartedAtUtc = DateTime.UtcNow;

        // Update room status
        if (task.Room is not null)
        {
            task.Room.CurrentStatus = RoomStatus.CleaningInProgress;
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(taskId);
    }

    public async Task<CleaningTaskDto?> CompleteAsync(Guid taskId)
    {
        var task = await _context.CleaningTasks
            .Include(ct => ct.Room)
            .FirstOrDefaultAsync(ct => ct.Id == taskId);

        if (task is null)
            return null;

        if (task.Status != CleaningTaskStatus.InProgress && task.Status != CleaningTaskStatus.Pending)
        {
            throw new InvalidOperationException("Only pending or in-progress tasks can be completed.");
        }

        task.Status = CleaningTaskStatus.Completed;
        task.CompletedAtUtc = DateTime.UtcNow;
        if (task.StartedAtUtc is null)
        {
            task.StartedAtUtc = DateTime.UtcNow;
        }

        // Update room status to Available
        if (task.Room is not null)
        {
            task.Room.CurrentStatus = RoomStatus.Available;
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(taskId);
    }

    public async Task<CleaningTaskDto?> SkipAsync(Guid taskId, string? reason = null)
    {
        var task = await _context.CleaningTasks.FindAsync(taskId);
        if (task is null)
            return null;

        if (task.Status == CleaningTaskStatus.Completed)
        {
            throw new InvalidOperationException("Completed tasks cannot be skipped.");
        }

        task.Status = CleaningTaskStatus.Skipped;
        if (reason is not null)
        {
            task.Notes = string.IsNullOrEmpty(task.Notes)
                ? $"Skipped: {reason}"
                : $"{task.Notes}\nSkipped: {reason}";
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(taskId);
    }

    private async Task UpdateTaskStatusAsync(CleaningTask task, CleaningTaskStatus newStatus)
    {
        switch (newStatus)
        {
            case CleaningTaskStatus.InProgress:
                if (task.Status != CleaningTaskStatus.Pending)
                {
                    throw new InvalidOperationException("Only pending tasks can be started.");
                }
                task.Status = CleaningTaskStatus.InProgress;
                task.StartedAtUtc = DateTime.UtcNow;
                if (task.Room is not null)
                {
                    task.Room.CurrentStatus = RoomStatus.CleaningInProgress;
                }
                break;

            case CleaningTaskStatus.Completed:
                task.Status = CleaningTaskStatus.Completed;
                task.CompletedAtUtc = DateTime.UtcNow;
                if (task.StartedAtUtc is null)
                {
                    task.StartedAtUtc = DateTime.UtcNow;
                }
                if (task.Room is not null)
                {
                    task.Room.CurrentStatus = RoomStatus.Available;
                }
                break;

            case CleaningTaskStatus.Skipped:
                if (task.Status == CleaningTaskStatus.Completed)
                {
                    throw new InvalidOperationException("Completed tasks cannot be skipped.");
                }
                task.Status = CleaningTaskStatus.Skipped;
                break;

            case CleaningTaskStatus.Pending:
                task.Status = CleaningTaskStatus.Pending;
                task.StartedAtUtc = null;
                task.CompletedAtUtc = null;
                break;
        }
    }

    private static CleaningTaskDto MapToDto(CleaningTask ct) => new(
        ct.Id,
        ct.RoomId,
        ct.Room?.RoomNumber ?? "Unknown",
        ct.ScheduledDate,
        ct.TaskType,
        ct.Status,
        ct.AssignedToStaffId,
        ct.AssignedToStaff?.DisplayName,
        ct.StartedAtUtc,
        ct.CompletedAtUtc,
        ct.Notes,
        ct.CreatedAtUtc);
}
