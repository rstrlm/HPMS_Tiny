using Pms.Domain.Enums;

namespace Pms.Application.DTOs;

public record CleaningTaskDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    DateOnly ScheduledDate,
    CleaningTaskType TaskType,
    CleaningTaskStatus Status,
    Guid? AssignedToStaffId,
    string? AssignedToStaffName,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? Notes,
    DateTime CreatedAtUtc);

public record CreateCleaningTaskRequest(
    Guid RoomId,
    DateOnly ScheduledDate,
    CleaningTaskType TaskType,
    Guid? AssignedToStaffId = null,
    string? Notes = null);

public record UpdateCleaningTaskRequest(
    Guid? AssignedToStaffId,
    CleaningTaskStatus? Status,
    string? Notes);

public record GenerateCleaningTasksRequest(
    DateOnly Date);

public record CleaningTaskSummaryDto(
    DateOnly Date,
    int TotalTasks,
    int Pending,
    int InProgress,
    int Completed,
    int Skipped);
