using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeService _roomTypeService;

    public RoomTypesController(IRoomTypeService roomTypeService)
    {
        _roomTypeService = roomTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomTypeDto>>> GetAll()
    {
        var roomTypes = await _roomTypeService.GetAllAsync();
        return Ok(roomTypes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomTypeDto>> GetById(Guid id)
    {
        var roomType = await _roomTypeService.GetByIdAsync(id);
        if (roomType is null)
            return NotFound();

        return Ok(roomType);
    }

    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<RoomTypeDto>> Create([FromBody] CreateRoomTypeRequest request)
    {
        var roomType = await _roomTypeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = roomType.Id }, roomType);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<RoomTypeDto>> Update(Guid id, [FromBody] UpdateRoomTypeRequest request)
    {
        var roomType = await _roomTypeService.UpdateAsync(id, request);
        if (roomType is null)
            return NotFound();

        return Ok(roomType);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roomTypeService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
