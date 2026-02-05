namespace Pms.Application.Interfaces;

public interface IKeycloakAdminService
{
    /// <summary>
    /// Creates a new user in Keycloak and returns their ID.
    /// </summary>
    Task<string> CreateUserAsync(CreateKeycloakUserRequest request);

    /// <summary>
    /// Assigns realm roles to a user.
    /// </summary>
    Task AssignRolesAsync(string userId, IEnumerable<string> roles);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    Task<KeycloakUserDto?> GetUserAsync(string userId);

    /// <summary>
    /// Updates a user's enabled status.
    /// </summary>
    Task SetUserEnabledAsync(string userId, bool enabled);

    /// <summary>
    /// Resets a user's password.
    /// </summary>
    Task ResetPasswordAsync(string userId, string newPassword, bool temporary = false);
}

public record CreateKeycloakUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string? Password = null,
    bool Enabled = true,
    bool EmailVerified = true);

public record KeycloakUserDto(
    string Id,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    bool Enabled);
