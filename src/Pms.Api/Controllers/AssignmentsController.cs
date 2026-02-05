using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "RequireFrontdesk")]
public class AssignmentsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public AssignmentsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _reservationService.RemoveRoomAssignmentAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
