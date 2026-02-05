using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/treatmentRooms")]
[Authorize]
public class TreatmentRoomsController : ControllerBase
{
    private readonly ITreatmentRoomService _treatmentRoomService;
    private readonly ITreatmentAvailabilityService _availabilityService;

    public TreatmentRoomsController(ITreatmentRoomService treatmentRoomService, ITreatmentAvailabilityService availabilityService)
    {
        _treatmentRoomService = treatmentRoomService;
        _availabilityService = availabilityService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TreatmentRoomDto>>> GetAll([FromQuery] bool? activeOnly)
    {
        var rooms = await _treatmentRoomService.GetAllAsync(activeOnly);
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TreatmentRoomDto>> GetById(Guid id)
    {
        var room = await _treatmentRoomService.GetByIdAsync(id);
        if (room is null)
            return NotFound();

        return Ok(room);
    }

    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<TreatmentRoomDto>> Create([FromBody] CreateTreatmentRoomRequest request)
    {
        var room = await _treatmentRoomService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<TreatmentRoomDto>> Update(Guid id, [FromBody] UpdateTreatmentRoomRequest request)
    {
        var room = await _treatmentRoomService.UpdateAsync(id, request);
        if (room is null)
            return NotFound();

        return Ok(room);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _treatmentRoomService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<IEnumerable<TimeSlot>>> GetAvailability(
        Guid id,
        [FromQuery] DateOnly date,
        [FromQuery] int durationMinutes,
        [FromQuery] int seats = 1)
    {
        var slots = await _availabilityService.GetAvailableTimeSlotsAsync(id, date, durationMinutes, seats);
        return Ok(slots);
    }
}
