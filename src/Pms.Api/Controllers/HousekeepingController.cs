using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Enums;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/housekeeping")]
[Authorize(Policy = "RequireCleaner")]
public class HousekeepingController : ControllerBase
{
    private readonly IHousekeepingService _housekeepingService;

    public HousekeepingController(IHousekeepingService housekeepingService)
    {
        _housekeepingService = housekeepingService;
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GetTasks(
        [FromQuery] DateOnly date,
        [FromQuery] CleaningTaskStatus? status = null,
        [FromQuery] Guid? assignedToStaffId = null)
    {
        var tasks = await _housekeepingService.GetTasksAsync(date, status, assignedToStaffId);
        return Ok(tasks);
    }

    [HttpGet("tasks/{id:guid}")]
    public async Task<ActionResult<CleaningTaskDto>> GetTask(Guid id)
    {
        var task = await _housekeepingService.GetByIdAsync(id);
        if (task is null)
            return NotFound();

        return Ok(task);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CleaningTaskSummaryDto>> GetSummary([FromQuery] DateOnly date)
    {
        var summary = await _housekeepingService.GetSummaryAsync(date);
        return Ok(summary);
    }

    [HttpPost("tasks")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<CleaningTaskDto>> CreateTask([FromBody] CreateCleaningTaskRequest request)
    {
        try
        {
            var task = await _housekeepingService.CreateAsync(request);
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("tasks/generate")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<IEnumerable<CleaningTaskDto>>> GenerateTasks([FromQuery] DateOnly date)
    {
        var tasks = await _housekeepingService.GenerateTasksForDateAsync(date);
        return Ok(tasks);
    }

    [HttpPatch("tasks/{id:guid}")]
    public async Task<ActionResult<CleaningTaskDto>> UpdateTask(Guid id, [FromBody] UpdateCleaningTaskRequest request)
    {
        try
        {
            var task = await _housekeepingService.UpdateAsync(id, request);
            if (task is null)
                return NotFound();

            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("tasks/{id:guid}/assign")]
    public async Task<ActionResult<CleaningTaskDto>> AssignTask(Guid id, [FromBody] AssignTaskRequest request)
    {
        try
        {
            var task = await _housekeepingService.AssignAsync(id, request.StaffId);
            if (task is null)
                return NotFound();

            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("tasks/{id:guid}/start")]
    public async Task<ActionResult<CleaningTaskDto>> StartTask(Guid id)
    {
        try
        {
            var task = await _housekeepingService.StartAsync(id);
            if (task is null)
                return NotFound();

            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("tasks/{id:guid}/complete")]
    public async Task<ActionResult<CleaningTaskDto>> CompleteTask(Guid id)
    {
        try
        {
            var task = await _housekeepingService.CompleteAsync(id);
            if (task is null)
                return NotFound();

            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("tasks/{id:guid}/skip")]
    public async Task<ActionResult<CleaningTaskDto>> SkipTask(Guid id, [FromBody] SkipTaskRequest? request = null)
    {
        try
        {
            var task = await _housekeepingService.SkipAsync(id, request?.Reason);
            if (task is null)
                return NotFound();

            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record AssignTaskRequest(Guid StaffId);
public record SkipTaskRequest(string? Reason);
