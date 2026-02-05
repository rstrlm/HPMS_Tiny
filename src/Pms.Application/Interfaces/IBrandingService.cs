using Pms.Application.DTOs;
using Pms.Application.Settings;

namespace Pms.Application.Interfaces;

public interface IBrandingService
{
    Task<BrandingDto> GetAsync();
    Task<BrandingSettings> GetSettingsAsync();
    Task<BrandingDto> UpdateAsync(UpdateBrandingRequest request, Guid? staffId, string? keycloakId);
    Task<IEnumerable<BrandingChangeLogDto>> GetChangeHistoryAsync();
}
