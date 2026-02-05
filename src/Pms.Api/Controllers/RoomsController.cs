using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly IRoomStateBlockService _roomStateBlockService;

    public RoomsController(IRoomService roomService, IRoomStateBlockService roomStateBlockService)
    {
        _roomService = roomService;
        _roomStateBlockService = roomStateBlockService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll([FromQuery] bool? activeOnly)
    {
        var rooms = await _roomService.GetAllAsync(activeOnly);
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomDto>> GetById(Guid id)
    {
        var room = await _roomService.GetByIdAsync(id);
        if (room is null)
            return NotFound();

        return Ok(room);
    }

    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomRequest request)
    {
        var room = await _roomService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<RoomDto>> Update(Guid id, [FromBody] UpdateRoomRequest request)
    {
        var room = await _roomService.UpdateAsync(id, request);
        if (room is null)
            return NotFound();

        return Ok(room);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roomService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    // Room State Blocks (nested under rooms)
    [HttpGet("{roomId:guid}/blocks")]
    public async Task<ActionResult<IEnumerable<RoomStateBlockDto>>> GetBlocks(
        Guid roomId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var blocks = await _roomStateBlockService.GetByRoomAsync(roomId, from, to);
        return Ok(blocks);
    }

    [HttpPost("{roomId:guid}/blocks")]
    [Authorize(Policy = "RequireMaintenance")]
    public async Task<ActionResult<RoomStateBlockDto>> CreateBlock(
        Guid roomId,
        [FromBody] CreateRoomStateBlockRequest request)
    {
        // TODO: Get staff ID from JWT claims
        var block = await _roomStateBlockService.CreateAsync(roomId, request);
        return CreatedAtAction(nameof(GetBlockById), new { roomId, blockId = block.Id }, block);
    }

    [HttpGet("{roomId:guid}/blocks/{blockId:guid}")]
    public async Task<ActionResult<RoomStateBlockDto>> GetBlockById(Guid roomId, Guid blockId)
    {
        var block = await _roomStateBlockService.GetByIdAsync(blockId);
        if (block is null || block.RoomId != roomId)
            return NotFound();

        return Ok(block);
    }
}
