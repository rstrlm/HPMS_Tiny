using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Enums;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireFrontdesk")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;
    private readonly IRoomAvailabilityService _availabilityService;

    public ReservationsController(IReservationService reservationService, IRoomAvailabilityService availabilityService)
    {
        _reservationService = reservationService;
        _availabilityService = availabilityService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetAll(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] ReservationStatus? status)
    {
        var reservations = await _reservationService.GetAllAsync(from, to, status);
        return Ok(reservations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id)
    {
        var reservation = await _reservationService.GetByIdAsync(id);
        if (reservation is null)
            return NotFound();

        return Ok(reservation);
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create([FromBody] CreateReservationRequest request)
    {
        try
        {
            var reservation = await _reservationService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, reservation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ReservationDto>> Update(Guid id, [FromBody] UpdateReservationRequest request)
    {
        try
        {
            var reservation = await _reservationService.UpdateAsync(id, request);
            if (reservation is null)
                return NotFound();

            return Ok(reservation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ReservationDto>> ChangeStatus(Guid id, [FromBody] ChangeReservationStatusRequest request)
    {
        var reservation = await _reservationService.ChangeStatusAsync(id, request.Status);
        if (reservation is null)
            return NotFound();

        return Ok(reservation);
    }

    [HttpPost("{id:guid}/assignments")]
    public async Task<ActionResult<RoomAssignmentDto>> AddAssignment(Guid id, [FromBody] CreateRoomAssignmentRequest request)
    {
        try
        {
            var assignment = await _reservationService.AddRoomAssignmentAsync(id, request);
            if (assignment is null)
                return NotFound();

            return CreatedAtAction(nameof(GetById), new { id }, assignment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("availability")]
    public async Task<ActionResult<IEnumerable<RoomAvailabilityInfo>>> GetAvailability(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] Guid? roomTypeId)
    {
        var availability = await _availabilityService.GetRoomAvailabilityAsync(from, to, roomTypeId);
        return Ok(availability);
    }

    [HttpPost("holds")]
    public async Task<ActionResult<HoldDto>> PlaceHold([FromBody] PlaceHoldRequest request)
    {
        // Check availability first
        var isAvailable = await _availabilityService.IsRoomAvailableAsync(request.RoomId, request.FromDate, request.ToDate);
        if (!isAvailable)
        {
            return BadRequest(new { error = "Room is not available for the requested dates." });
        }

        var holdId = await _availabilityService.PlaceHoldAsync(
            request.RoomId,
            request.FromDate,
            request.ToDate,
            staffId: null, // TODO: Get from JWT claims
            sessionId: null,
            request.HoldMinutes);

        return Ok(new HoldDto(
            holdId,
            request.RoomId,
            request.FromDate,
            request.ToDate,
            DateTime.UtcNow.AddMinutes(request.HoldMinutes)));
    }

    [HttpDelete("holds/{holdId:guid}")]
    public async Task<IActionResult> ReleaseHold(Guid holdId)
    {
        var result = await _availabilityService.ReleaseHoldAsync(holdId);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
