using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize(Policy = "RequireFrontdesk")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StaffProfileDto>>> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] string? search)
    {
        var staff = await _staffService.GetAllAsync(activeOnly, search);
        return Ok(staff);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StaffProfileDto>> GetById(Guid id)
    {
        var staff = await _staffService.GetByIdAsync(id);
        if (staff is null)
            return NotFound();

        return Ok(staff);
    }

    [HttpGet("by-keycloak/{keycloakUserId}")]
    public async Task<ActionResult<StaffProfileDto>> GetByKeycloakUserId(string keycloakUserId)
    {
        var staff = await _staffService.GetByKeycloakUserIdAsync(keycloakUserId);
        if (staff is null)
            return NotFound();

        return Ok(staff);
    }

    /// <summary>
    /// Creates a staff profile for an existing Keycloak user.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<StaffProfileDto>> Create([FromBody] CreateStaffProfileRequest request)
    {
        try
        {
            var staff = await _staffService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = staff.Id }, staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new user in Keycloak AND creates their staff profile in PMS.
    /// </summary>
    [HttpPost("with-keycloak")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<StaffProfileDto>> CreateWithKeycloak([FromBody] CreateStaffWithKeycloakRequest request)
    {
        try
        {
            var staff = await _staffService.CreateWithKeycloakAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = staff.Id }, staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<StaffProfileDto>> Update(Guid id, [FromBody] UpdateStaffProfileRequest request)
    {
        var staff = await _staffService.UpdateAsync(id, request);
        if (staff is null)
            return NotFound();

        return Ok(staff);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _staffService.DeactivateAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
