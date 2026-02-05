namespace Pms.Application.DTOs;

public record BrandingDto(
    Guid Id,
    string CompanyName,
    string CompanyLegalName,
    string Tagline,
    string Address,
    string Email,
    string Phone,
    string TaxId,
    string BankName,
    string IBAN,
    string BIC,
    DateTime UpdatedAtUtc);

public record UpdateBrandingRequest(
    string CompanyName,
    string CompanyLegalName,
    string Tagline,
    string Address,
    string Email,
    string Phone,
    string TaxId,
    string BankName,
    string IBAN,
    string BIC);

public record BrandingChangeLogDto(
    Guid Id,
    string? OldValues,
    string? NewValues,
    Guid? PerformedByStaffId,
    string? PerformedByKeycloakId,
    DateTime CreatedAtUtc);
