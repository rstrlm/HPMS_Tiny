using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly PmsDbContext _context;
    private readonly IKeycloakAdminService? _keycloakAdmin;

    public StaffService(PmsDbContext context, IKeycloakAdminService? keycloakAdmin = null)
    {
        _context = context;
        _keycloakAdmin = keycloakAdmin;
    }

    public async Task<StaffProfileDto?> GetByIdAsync(Guid id)
    {
        var staff = await _context.StaffProfiles.FindAsync(id);
        return staff is null ? null : MapToDto(staff);
    }

    public async Task<StaffProfileDto?> GetByKeycloakUserIdAsync(string keycloakUserId)
    {
        var staff = await _context.StaffProfiles
            .FirstOrDefaultAsync(s => s.KeycloakUserId == keycloakUserId);
        return staff is null ? null : MapToDto(staff);
    }

    public async Task<IEnumerable<StaffProfileDto>> GetAllAsync(bool? activeOnly = null, string? search = null)
    {
        var query = _context.StaffProfiles.AsQueryable();

        if (activeOnly == true)
        {
            query = query.Where(s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(s =>
                s.DisplayName.ToLower().Contains(searchLower) ||
                (s.Email != null && s.Email.ToLower().Contains(searchLower)));
        }

        var staffList = await query
            .OrderBy(s => s.DisplayName)
            .ToListAsync();

        return staffList.Select(MapToDto);
    }

    public async Task<StaffProfileDto> CreateAsync(CreateStaffProfileRequest request)
    {
        // Check if KeycloakUserId already exists
        var existing = await _context.StaffProfiles
            .FirstOrDefaultAsync(s => s.KeycloakUserId == request.KeycloakUserId);

        if (existing is not null)
        {
            throw new InvalidOperationException($"Staff profile for Keycloak user '{request.KeycloakUserId}' already exists.");
        }

        var staff = new StaffProfile
        {
            KeycloakUserId = request.KeycloakUserId,
            DisplayName = request.DisplayName,
            Email = request.Email,
            Skills = request.Skills,
            IsActive = true
        };

        _context.StaffProfiles.Add(staff);
        await _context.SaveChangesAsync();

        return MapToDto(staff);
    }

    public async Task<StaffProfileDto> CreateWithKeycloakAsync(CreateStaffWithKeycloakRequest request)
    {
        if (_keycloakAdmin is null)
        {
            throw new InvalidOperationException("Keycloak admin service is not configured.");
        }

        // Parse display name into first/last name
        var nameParts = request.DisplayName.Split(' ', 2);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : "";

        // Create user in Keycloak
        var keycloakUserId = await _keycloakAdmin.CreateUserAsync(new CreateKeycloakUserRequest(
            Username: request.Username,
            Email: request.Email,
            FirstName: firstName,
            LastName: lastName,
            Password: request.Password,
            Enabled: true,
            EmailVerified: true
        ));

        // Assign roles if provided
        if (request.Roles?.Any() == true)
        {
            await _keycloakAdmin.AssignRolesAsync(keycloakUserId, request.Roles);
        }

        // Create staff profile in PMS
        var staff = new StaffProfile
        {
            KeycloakUserId = keycloakUserId,
            DisplayName = request.DisplayName,
            Email = request.Email,
            Skills = request.Skills,
            IsActive = true
        };

        _context.StaffProfiles.Add(staff);
        await _context.SaveChangesAsync();

        return MapToDto(staff);
    }

    public async Task<StaffProfileDto?> UpdateAsync(Guid id, UpdateStaffProfileRequest request)
    {
        var staff = await _context.StaffProfiles.FindAsync(id);
        if (staff is null)
            return null;

        if (request.DisplayName is not null)
            staff.DisplayName = request.DisplayName;

        if (request.Email is not null)
            staff.Email = request.Email;

        if (request.Skills is not null)
            staff.Skills = request.Skills;

        if (request.IsActive.HasValue)
            staff.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return MapToDto(staff);
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var staff = await _context.StaffProfiles.FindAsync(id);
        if (staff is null)
            return false;

        staff.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    private static StaffProfileDto MapToDto(StaffProfile s) => new(
        s.Id,
        s.KeycloakUserId,
        s.DisplayName,
        s.Email,
        s.Skills,
        s.IsActive,
        s.CreatedAtUtc,
        s.UpdatedAtUtc);
}
