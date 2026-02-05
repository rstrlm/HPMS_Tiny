using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireMaintenance")]
public class BlocksController : ControllerBase
{
    private readonly IRoomStateBlockService _roomStateBlockService;

    public BlocksController(IRoomStateBlockService roomStateBlockService)
    {
        _roomStateBlockService = roomStateBlockService;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roomStateBlockService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
