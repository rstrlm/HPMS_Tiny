using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/treatments/types")]
[Authorize]
public class TreatmentTypesController : ControllerBase
{
    private readonly ITreatmentTypeService _treatmentTypeService;

    public TreatmentTypesController(ITreatmentTypeService treatmentTypeService)
    {
        _treatmentTypeService = treatmentTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TreatmentTypeDto>>> GetAll([FromQuery] bool? activeOnly)
    {
        var types = await _treatmentTypeService.GetAllAsync(activeOnly);
        return Ok(types);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TreatmentTypeDto>> GetById(Guid id)
    {
        var type = await _treatmentTypeService.GetByIdAsync(id);
        if (type is null)
            return NotFound();

        return Ok(type);
    }

    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<TreatmentTypeDto>> Create([FromBody] CreateTreatmentTypeRequest request)
    {
        var type = await _treatmentTypeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<TreatmentTypeDto>> Update(Guid id, [FromBody] UpdateTreatmentTypeRequest request)
    {
        var type = await _treatmentTypeService.UpdateAsync(id, request);
        if (type is null)
            return NotFound();

        return Ok(type);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _treatmentTypeService.DeleteAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
