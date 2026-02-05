using Pms.Application.DTOs;
using Pms.Domain.Enums;

namespace Pms.Application.Interfaces;

public interface IHousekeepingService
{
    Task<CleaningTaskDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CleaningTaskDto>> GetTasksAsync(DateOnly date, CleaningTaskStatus? status = null, Guid? assignedToStaffId = null);
    Task<CleaningTaskSummaryDto> GetSummaryAsync(DateOnly date);

    /// <summary>
    /// Creates a single cleaning task.
    /// </summary>
    Task<CleaningTaskDto> CreateAsync(CreateCleaningTaskRequest request);

    /// <summary>
    /// Generates cleaning tasks for a given date.
    /// - Checkout tasks: rooms with checkout on that date
    /// - Stayover tasks: occupied rooms without checkout
    /// </summary>
    Task<IEnumerable<CleaningTaskDto>> GenerateTasksForDateAsync(DateOnly date);

    /// <summary>
    /// Updates a cleaning task. Handles status transitions and room status updates.
    /// </summary>
    Task<CleaningTaskDto?> UpdateAsync(Guid id, UpdateCleaningTaskRequest request);

    /// <summary>
    /// Assigns a staff member to a task.
    /// </summary>
    Task<CleaningTaskDto?> AssignAsync(Guid taskId, Guid staffId);

    /// <summary>
    /// Starts a cleaning task. Updates room status to CleaningInProgress.
    /// </summary>
    Task<CleaningTaskDto?> StartAsync(Guid taskId);

    /// <summary>
    /// Completes a cleaning task. Updates room status to Available.
    /// </summary>
    Task<CleaningTaskDto?> CompleteAsync(Guid taskId);

    /// <summary>
    /// Skips a cleaning task.
    /// </summary>
    Task<CleaningTaskDto?> SkipAsync(Guid taskId, string? reason = null);
}
