namespace Pms.Application.DTOs;

public record CustomerDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes);

public record UpdateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes);
