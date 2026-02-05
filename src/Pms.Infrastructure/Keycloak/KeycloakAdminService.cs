using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Pms.Application.Interfaces;

namespace Pms.Infrastructure.Keycloak;

public class KeycloakAdminSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string Realm { get; set; } = "pms";
    public string AdminRealm { get; set; } = "master";
    public string ClientId { get; set; } = "admin-cli";
    public string AdminUsername { get; set; } = "admin";
    public string AdminPassword { get; set; } = "admin";
}

public class KeycloakAdminService : IKeycloakAdminService
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakAdminSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public KeycloakAdminService(HttpClient httpClient, IOptions<KeycloakAdminSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<string> CreateUserAsync(CreateKeycloakUserRequest request)
    {
        await EnsureAuthenticatedAsync();

        var keycloakUser = new
        {
            username = request.Username,
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            enabled = request.Enabled,
            emailVerified = request.EmailVerified,
            credentials = request.Password is not null
                ? new[]
                {
                    new
                    {
                        type = "password",
                        value = request.Password,
                        temporary = false
                    }
                }
                : null
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrl}/admin/realms/{_settings.Realm}/users",
            keycloakUser,
            _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create user in Keycloak: {error}");
        }

        // Get the user ID from the Location header
        var locationHeader = response.Headers.Location?.ToString();
        if (locationHeader is null)
        {
            throw new InvalidOperationException("Keycloak did not return the user location");
        }

        var userId = locationHeader.Split('/').Last();
        return userId;
    }

    public async Task AssignRolesAsync(string userId, IEnumerable<string> roles)
    {
        await EnsureAuthenticatedAsync();

        // Get available realm roles
        var rolesResponse = await _httpClient.GetAsync(
            $"{_settings.BaseUrl}/admin/realms/{_settings.Realm}/roles");

        if (!rolesResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to get realm roles from Keycloak");
        }

        var availableRoles = await rolesResponse.Content.ReadFromJsonAsync<List<KeycloakRole>>(_jsonOptions);
        if (availableRoles is null) return;

        // Filter to only the roles we want to assign
        var rolesToAssign = availableRoles
            .Where(r => roles.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (rolesToAssign.Count == 0) return;

        // Assign roles to user
        var assignResponse = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrl}/admin/realms/{_settings.Realm}/users/{userId}/role-mappings/realm",
            rolesToAssign,
            _jsonOptions);

        if (!assignResponse.IsSuccessStatusCode)
        {
            var error = await assignResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to assign roles in Keycloak: {error}");
        }
    }

    public async Task<KeycloakUserDto?> GetUserAsync(string userId)
    {
        await EnsureAuthenticatedAsync();

        var response = await _httpClient.GetAsync(
            $"{_settings.BaseUrl}/admin/realms/{_settings.Realm}/users/{userId}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var user = await response.Content.ReadFromJsonAsync<KeycloakUserResponse>(_jsonOptions);
        if (user is null) return null;

        return new KeycloakUserDto(
            user.Id,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Enabled);
    }

    public async Task SetUserEnabledAsync(string userId, bool enabled)
    {
        await EnsureAuthenticatedAsync();

        var update = new { enabled };
        var response = await _httpClient.PutAsJsonAsync(
            $"{_settings.BaseUrl}/admin/realms/{_settings.Realm}/users/{userId}",
            update,
            _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to update user in Keycloak: {error}");
        }
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, bool temporary = false)
    {
        await EnsureAuthenticatedAsync();

        var credential = new
        {
            type = "password",
            value = newPassword,
            temporary
        };

        var response = await _httpClient.PutAsJsonAsync(
            $"{_settings.BaseUrl}/admin/realms/{_settings.Realm}/users/{userId}/reset-password",
            credential,
            _jsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to reset password in Keycloak: {error}");
        }
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_accessToken is not null && DateTime.UtcNow < _tokenExpiry)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
            return;
        }

        // Get a new token using admin credentials
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _settings.ClientId,
            ["username"] = _settings.AdminUsername,
            ["password"] = _settings.AdminPassword
        });

        HttpResponseMessage response;
        var tokenUrl = $"{_settings.BaseUrl}/realms/{_settings.AdminRealm}/protocol/openid-connect/token";
        try
        {
            response = await _httpClient.PostAsync(tokenUrl, tokenRequest);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to Keycloak at {_settings.BaseUrl}. Is Keycloak running? Error: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Keycloak authentication failed (HTTP {(int)response.StatusCode} from {tokenUrl}): {error}");
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions);
        if (tokenResponse is null)
        {
            throw new InvalidOperationException("Failed to parse Keycloak token response");
        }

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30); // 30 second buffer

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private record KeycloakRole(string Id, string Name);

    private record KeycloakUserResponse(
        string Id,
        string Username,
        string? Email,
        string? FirstName,
        string? LastName,
        bool Enabled);
}
