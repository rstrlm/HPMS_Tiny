using Pms.Application.DTOs;

namespace Pms.Application.Interfaces;

public interface IFolioService
{
    Task<FolioDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<FolioSummaryDto>> GetByCustomerAsync(Guid customerId);
    Task<FolioDto?> GetByReservationAsync(Guid reservationId);

    /// <summary>
    /// Creates a new folio for a customer, optionally linked to a reservation.
    /// </summary>
    Task<FolioDto> CreateAsync(CreateFolioRequest request);

    /// <summary>
    /// Adds a charge to a folio. Fails if folio is closed.
    /// </summary>
    Task<ChargeDto> AddChargeAsync(Guid folioId, CreateChargeRequest request);

    /// <summary>
    /// Removes a charge from a folio. Fails if folio is closed.
    /// </summary>
    Task<bool> RemoveChargeAsync(Guid chargeId);

    /// <summary>
    /// Records a payment against a folio.
    /// </summary>
    Task<PaymentDto> AddPaymentAsync(Guid folioId, CreatePaymentRequest request);

    /// <summary>
    /// Issues an invoice for the folio. Creates a snapshot of current totals.
    /// </summary>
    Task<InvoiceDto> IssueInvoiceAsync(Guid folioId);

    /// <summary>
    /// Closes the folio. Fails if balance is not zero.
    /// </summary>
    Task<FolioDto?> CloseFolioAsync(Guid folioId);

    /// <summary>
    /// Cancels a folio. Used when a reservation is cancelled.
    /// </summary>
    Task<FolioDto?> CancelFolioAsync(Guid folioId);

    /// <summary>
    /// Voids an invoice.
    /// </summary>
    Task<InvoiceDto?> VoidInvoiceAsync(Guid invoiceId);

    /// <summary>
    /// Gets all invoices for a folio.
    /// </summary>
    Task<IEnumerable<InvoiceDto>> GetInvoicesByFolioAsync(Guid folioId);

    /// <summary>
    /// Merges multiple folios into a target folio.
    /// Moves all charges and payments from source folios to target.
    /// Source folios are closed after merge.
    /// </summary>
    Task<FolioDto> MergeFoliosAsync(Guid targetFolioId, IEnumerable<Guid> sourceFolioIds);

    /// <summary>
    /// Generates a PDF for an invoice.
    /// </summary>
    Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId);
}
