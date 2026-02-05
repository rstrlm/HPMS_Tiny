using Pms.Domain.Enums;

namespace Pms.Application.DTOs;

// Folio DTOs
public record FolioDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? ReservationId,
    FolioStatus Status,
    decimal SubTotal,
    decimal VatTotal,
    decimal GrandTotal,
    decimal TotalPaid,
    decimal Balance,
    IEnumerable<ChargeDto> Charges,
    IEnumerable<PaymentDto> Payments,
    DateTime CreatedAtUtc);

public record FolioSummaryDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? ReservationId,
    FolioStatus Status,
    decimal GrandTotal,
    decimal TotalPaid,
    decimal Balance,
    DateTime CreatedAtUtc);

public record CreateFolioRequest(
    Guid CustomerId,
    Guid? ReservationId = null);

// Charge DTOs
public record ChargeDto(
    Guid Id,
    ChargeType Type,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal VatRate,
    decimal SubTotal,
    decimal VatAmount,
    decimal Total,
    DateTime CreatedAtUtc);

public record CreateChargeRequest(
    ChargeType Type,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal VatRate = 0.24m);

// Payment DTOs
public record PaymentDto(
    Guid Id,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? ProviderReference,
    DateTime CreatedAtUtc);

public record CreatePaymentRequest(
    decimal Amount,
    PaymentMethod Method,
    string? ProviderReference = null);

// Invoice DTOs
public record InvoiceDto(
    Guid Id,
    Guid FolioId,
    string InvoiceNumber,
    DateTime IssuedAtUtc,
    InvoiceStatus Status,
    decimal SubTotal,
    decimal VatTotal,
    decimal GrandTotal);

public record IssueInvoiceRequest(
    Guid FolioId);

public record MergeFoliosRequest(
    Guid TargetFolioId,
    IEnumerable<Guid> SourceFolioIds);
