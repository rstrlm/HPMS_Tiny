namespace Pms.Application.DTOs;

public record StaffProfileDto(
    Guid Id,
    string KeycloakUserId,
    string DisplayName,
    string? Email,
    string? Skills,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>
/// Creates a staff profile for an existing Keycloak user.
/// </summary>
public record CreateStaffProfileRequest(
    string KeycloakUserId,
    string DisplayName,
    string? Email,
    string? Skills);

/// <summary>
/// Creates a new user in Keycloak AND creates their staff profile in PMS.
/// </summary>
public record CreateStaffWithKeycloakRequest(
    string Username,
    string Password,
    string DisplayName,
    string Email,
    string? Skills,
    IEnumerable<string>? Roles);

public record UpdateStaffProfileRequest(
    string? DisplayName,
    string? Email,
    string? Skills,
    bool? IsActive);
