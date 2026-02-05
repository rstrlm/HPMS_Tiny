using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface IStaffService
{
    Task<StaffProfileDto?> GetByIdAsync(Guid id);
    Task<StaffProfileDto?> GetByKeycloakUserIdAsync(string keycloakUserId);
    Task<IEnumerable<StaffProfileDto>> GetAllAsync(bool? activeOnly = null, string? search = null);

    /// <summary>
    /// Creates a new staff profile linked to an existing Keycloak user.
    /// </summary>
    Task<StaffProfileDto> CreateAsync(CreateStaffProfileRequest request);

    /// <summary>
    /// Creates a new user in Keycloak AND creates their staff profile in PMS.
    /// </summary>
    Task<StaffProfileDto> CreateWithKeycloakAsync(CreateStaffWithKeycloakRequest request);

    Task<StaffProfileDto?> UpdateAsync(Guid id, UpdateStaffProfileRequest request);

    /// <summary>
    /// Deactivates a staff profile (soft delete).
    /// </summary>
    Task<bool> DeactivateAsync(Guid id);
}
