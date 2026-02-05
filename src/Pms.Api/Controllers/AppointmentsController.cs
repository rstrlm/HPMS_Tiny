using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Api.Authorization;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireFrontdesk")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IAuthorizationService _authorizationService;

    public AppointmentsController(IAppointmentService appointmentService, IAuthorizationService authorizationService)
    {
        _appointmentService = appointmentService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? therapistId)
    {
        var appointments = await _appointmentService.GetAllAsync(from, to, therapistId);
        return Ok(appointments);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id)
    {
        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment is null)
            return NotFound();

        return Ok(appointment);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentRequest request)
    {
        try
        {
            var appointment = await _appointmentService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> Update(Guid id, [FromBody] UpdateAppointmentRequest request)
    {
        try
        {
            var appointment = await _appointmentService.UpdateAsync(id, request);
            if (appointment is null)
                return NotFound();

            return Ok(appointment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "RequireTherapist")]
    public async Task<ActionResult<AppointmentDto>> ChangeStatus(Guid id, [FromBody] ChangeAppointmentStatusRequest request)
    {
        // Resource-based authorization: therapists can only change status of their own appointments
        var authResult = await _authorizationService.AuthorizeAsync(User, id, new TherapistOwnAppointmentRequirement());
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var appointment = await _appointmentService.ChangeStatusAsync(id, request.Status);
        if (appointment is null)
            return NotFound();

        return Ok(appointment);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _appointmentService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
