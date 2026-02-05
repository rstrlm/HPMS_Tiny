using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/branding")]
public class BrandingController : ControllerBase
{
    private readonly IBrandingService _brandingService;

    public BrandingController(IBrandingService brandingService)
    {
        _brandingService = brandingService;
    }

    [HttpGet]
    public async Task<ActionResult<BrandingPublicResponse>> Get()
    {
        var branding = await _brandingService.GetAsync();
        return Ok(new BrandingPublicResponse(branding.CompanyName, branding.Tagline));
    }

    [HttpGet("full")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<BrandingDto>> GetFull()
    {
        return Ok(await _brandingService.GetAsync());
    }

    [HttpPut]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<BrandingDto>> Update([FromBody] UpdateBrandingRequest request)
    {
        var keycloakId = User.FindFirst("sub")?.Value;
        var result = await _brandingService.UpdateAsync(request, staffId: null, keycloakId: keycloakId);
        return Ok(result);
    }

    [HttpGet("history")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<IEnumerable<BrandingChangeLogDto>>> GetHistory()
    {
        return Ok(await _brandingService.GetChangeHistoryAsync());
    }
}

public record BrandingPublicResponse(string CompanyName, string Tagline);
