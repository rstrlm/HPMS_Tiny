using Microsoft.EntityFrameworkCore;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Infrastructure.Persistence;

namespace Pms.Infrastructure.Services;

public class FolioService : IFolioService
{
    private readonly PmsDbContext _context;
    private readonly IBrandingService _brandingService;

    public FolioService(PmsDbContext context, IBrandingService brandingService)
    {
        _context = context;
        _brandingService = brandingService;
    }

    public async Task<FolioDto?> GetByIdAsync(Guid id)
    {
        var folio = await _context.Folios
            .Include(f => f.Customer)
            .Include(f => f.Charges)
            .Include(f => f.Payments)
            .FirstOrDefaultAsync(f => f.Id == id);

        return folio is null ? null : MapToDto(folio);
    }

    public async Task<IEnumerable<FolioSummaryDto>> GetByCustomerAsync(Guid customerId)
    {
        var folios = await _context.Folios
            .Include(f => f.Customer)
            .Include(f => f.Charges)
            .Include(f => f.Payments)
            .Where(f => f.CustomerId == customerId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync();

        return folios.Select(MapToSummaryDto);
    }

    public async Task<FolioDto?> GetByReservationAsync(Guid reservationId)
    {
        var folio = await _context.Folios
            .Include(f => f.Customer)
            .Include(f => f.Charges)
            .Include(f => f.Payments)
            .FirstOrDefaultAsync(f => f.ReservationId == reservationId);

        return folio is null ? null : MapToDto(folio);
    }

    public async Task<FolioDto> CreateAsync(CreateFolioRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer not found.");
        }

        if (request.ReservationId.HasValue)
        {
            var reservation = await _context.Reservations.FindAsync(request.ReservationId.Value);
            if (reservation is null)
            {
                throw new InvalidOperationException("Reservation not found.");
            }

            // Check if folio already exists for this reservation
            var existingFolio = await _context.Folios
                .FirstOrDefaultAsync(f => f.ReservationId == request.ReservationId.Value);
            if (existingFolio is not null)
            {
                throw new InvalidOperationException("A folio already exists for this reservation.");
            }
        }

        var folio = new Folio
        {
            CustomerId = request.CustomerId,
            ReservationId = request.ReservationId,
            Status = FolioStatus.Open
        };

        _context.Folios.Add(folio);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(folio.Id))!;
    }

    public async Task<ChargeDto> AddChargeAsync(Guid folioId, CreateChargeRequest request)
    {
        var folio = await _context.Folios.FindAsync(folioId);
        if (folio is null)
        {
            throw new InvalidOperationException("Folio not found.");
        }

        if (folio.Status == FolioStatus.Closed)
        {
            throw new InvalidOperationException("Cannot add charges to a closed folio.");
        }

        var charge = new Charge
        {
            FolioId = folioId,
            Type = request.Type,
            Description = request.Description,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            VatRate = request.VatRate
        };

        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        return MapChargeToDto(charge);
    }

    public async Task<bool> RemoveChargeAsync(Guid chargeId)
    {
        var charge = await _context.Charges
            .Include(c => c.Folio)
            .FirstOrDefaultAsync(c => c.Id == chargeId);

        if (charge is null)
            return false;

        if (charge.Folio?.Status == FolioStatus.Closed)
        {
            throw new InvalidOperationException("Cannot remove charges from a closed folio.");
        }

        _context.Charges.Remove(charge);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PaymentDto> AddPaymentAsync(Guid folioId, CreatePaymentRequest request)
    {
        var folio = await _context.Folios.FindAsync(folioId);
        if (folio is null)
        {
            throw new InvalidOperationException("Folio not found.");
        }

        var payment = new Payment
        {
            FolioId = folioId,
            Amount = request.Amount,
            Method = request.Method,
            ProviderReference = request.ProviderReference,
            Status = PaymentStatus.Paid // For now, all payments are immediately confirmed
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return MapPaymentToDto(payment);
    }

    public async Task<InvoiceDto> IssueInvoiceAsync(Guid folioId)
    {
        var folio = await _context.Folios
            .Include(f => f.Charges)
            .FirstOrDefaultAsync(f => f.Id == folioId);

        if (folio is null)
        {
            throw new InvalidOperationException("Folio not found.");
        }

        if (!folio.Charges.Any())
        {
            throw new InvalidOperationException("Cannot issue invoice for folio with no charges.");
        }

        // Calculate totals
        var subTotal = folio.Charges.Sum(c => c.SubTotal);
        var vatTotal = folio.Charges.Sum(c => c.VatAmount);
        var grandTotal = subTotal + vatTotal;

        // Generate invoice number (simple sequential for now)
        var lastInvoice = await _context.Invoices
            .OrderByDescending(i => i.CreatedAtUtc)
            .FirstOrDefaultAsync();

        var invoiceNumber = GenerateInvoiceNumber(lastInvoice?.InvoiceNumber);

        var invoice = new Invoice
        {
            FolioId = folioId,
            InvoiceNumber = invoiceNumber,
            IssuedAtUtc = DateTime.UtcNow,
            Status = InvoiceStatus.Issued,
            SubTotal = subTotal,
            VatTotal = vatTotal,
            GrandTotal = grandTotal
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return MapInvoiceToDto(invoice);
    }

    public async Task<FolioDto?> CloseFolioAsync(Guid folioId)
    {
        var folio = await _context.Folios
            .Include(f => f.Charges)
            .Include(f => f.Payments)
            .FirstOrDefaultAsync(f => f.Id == folioId);

        if (folio is null)
            return null;

        if (folio.Status == FolioStatus.Closed)
        {
            throw new InvalidOperationException("Folio is already closed.");
        }

        var grandTotal = folio.Charges.Sum(c => c.Total);
        var totalPaid = folio.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .Sum(p => p.Amount);
        var balance = grandTotal - totalPaid;

        if (balance != 0)
        {
            throw new InvalidOperationException($"Cannot close folio with outstanding balance: {balance:C}");
        }

        folio.Status = FolioStatus.Closed;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(folioId);
    }

    public async Task<FolioDto?> CancelFolioAsync(Guid folioId)
    {
        var folio = await _context.Folios
            .Include(f => f.Charges)
            .Include(f => f.Payments)
            .FirstOrDefaultAsync(f => f.Id == folioId);

        if (folio is null)
            return null;

        if (folio.Status == FolioStatus.Closed)
        {
            throw new InvalidOperationException("Cannot cancel a closed folio.");
        }

        if (folio.Status == FolioStatus.Cancelled)
        {
            return await GetByIdAsync(folioId);
        }

        // Check if there are any payments - if so, they need to be refunded first
        var totalPaid = folio.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .Sum(p => p.Amount);

        if (totalPaid > 0)
        {
            throw new InvalidOperationException($"Cannot cancel folio with payments. Please refund {totalPaid:C} first.");
        }

        folio.Status = FolioStatus.Cancelled;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(folioId);
    }

    public async Task<InvoiceDto?> VoidInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice is null)
            return null;

        if (invoice.Status == InvoiceStatus.Voided)
        {
            throw new InvalidOperationException("Invoice is already voided.");
        }

        invoice.Status = InvoiceStatus.Voided;
        await _context.SaveChangesAsync();

        return MapInvoiceToDto(invoice);
    }

    public async Task<IEnumerable<InvoiceDto>> GetInvoicesByFolioAsync(Guid folioId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.FolioId == folioId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .ToListAsync();

        return invoices.Select(MapInvoiceToDto);
    }

    private static string GenerateInvoiceNumber(string? lastInvoiceNumber)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";

        if (lastInvoiceNumber is null || !lastInvoiceNumber.StartsWith(prefix))
        {
            return $"{prefix}00001";
        }

        var sequencePart = lastInvoiceNumber[prefix.Length..];
        if (int.TryParse(sequencePart, out var sequence))
        {
            return $"{prefix}{(sequence + 1):D5}";
        }

        return $"{prefix}00001";
    }

    private static FolioDto MapToDto(Folio f)
    {
        var charges = f.Charges.Select(MapChargeToDto).ToList();
        var payments = f.Payments.Select(MapPaymentToDto).ToList();

        var subTotal = f.Charges.Sum(c => c.SubTotal);
        var vatTotal = f.Charges.Sum(c => c.VatAmount);
        var grandTotal = subTotal + vatTotal;
        var totalPaid = f.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .Sum(p => p.Amount);
        var balance = grandTotal - totalPaid;

        return new FolioDto(
            f.Id,
            f.CustomerId,
            f.Customer?.Name ?? "Unknown",
            f.ReservationId,
            f.Status,
            subTotal,
            vatTotal,
            grandTotal,
            totalPaid,
            balance,
            charges,
            payments,
            f.CreatedAtUtc);
    }

    private static FolioSummaryDto MapToSummaryDto(Folio f)
    {
        var grandTotal = f.Charges.Sum(c => c.Total);
        var totalPaid = f.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .Sum(p => p.Amount);
        var balance = grandTotal - totalPaid;

        return new FolioSummaryDto(
            f.Id,
            f.CustomerId,
            f.Customer?.Name ?? "Unknown",
            f.ReservationId,
            f.Status,
            grandTotal,
            totalPaid,
            balance,
            f.CreatedAtUtc);
    }

    private static ChargeDto MapChargeToDto(Charge c) => new(
        c.Id,
        c.Type,
        c.Description,
        c.Quantity,
        c.UnitPrice,
        c.VatRate,
        c.SubTotal,
        c.VatAmount,
        c.Total,
        c.CreatedAtUtc);

    private static PaymentDto MapPaymentToDto(Payment p) => new(
        p.Id,
        p.Amount,
        p.Method,
        p.Status,
        p.ProviderReference,
        p.CreatedAtUtc);

    private static InvoiceDto MapInvoiceToDto(Invoice i) => new(
        i.Id,
        i.FolioId,
        i.InvoiceNumber,
        i.IssuedAtUtc,
        i.Status,
        i.SubTotal,
        i.VatTotal,
        i.GrandTotal);

    public async Task<FolioDto> MergeFoliosAsync(Guid targetFolioId, IEnumerable<Guid> sourceFolioIds)
    {
        var sourceIds = sourceFolioIds.ToList();

        if (sourceIds.Contains(targetFolioId))
        {
            throw new InvalidOperationException("Target folio cannot be in the source list.");
        }

        var targetFolio = await _context.Folios
            .Include(f => f.Customer)
            .FirstOrDefaultAsync(f => f.Id == targetFolioId);

        if (targetFolio is null)
        {
            throw new InvalidOperationException("Target folio not found.");
        }

        if (targetFolio.Status != FolioStatus.Open)
        {
            throw new InvalidOperationException("Target folio must be open to merge into.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var sourceId in sourceIds)
            {
                var sourceFolio = await _context.Folios
                    .Include(f => f.Charges)
                    .Include(f => f.Payments)
                    .FirstOrDefaultAsync(f => f.Id == sourceId);

                if (sourceFolio is null)
                {
                    throw new InvalidOperationException($"Source folio {sourceId} not found.");
                }

                if (sourceFolio.Status != FolioStatus.Open)
                {
                    throw new InvalidOperationException($"Source folio {sourceId} must be open to merge.");
                }

                // Must be same customer
                if (sourceFolio.CustomerId != targetFolio.CustomerId)
                {
                    throw new InvalidOperationException("All folios must belong to the same customer.");
                }

                // Move charges to target folio
                foreach (var charge in sourceFolio.Charges)
                {
                    charge.FolioId = targetFolioId;
                }

                // Move payments to target folio
                foreach (var payment in sourceFolio.Payments)
                {
                    payment.FolioId = targetFolioId;
                }

                // Close the source folio
                sourceFolio.Status = FolioStatus.Closed;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await GetByIdAsync(targetFolioId))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Folio)
                .ThenInclude(f => f!.Customer)
            .Include(i => i.Folio)
                .ThenInclude(f => f!.Charges)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice is null)
        {
            throw new InvalidOperationException("Invoice not found.");
        }

        var folio = invoice.Folio!;
        var customer = folio.Customer!;
        var charges = folio.Charges.ToList();

        var branding = await _brandingService.GetSettingsAsync();
        return InvoicePdfGenerator.Generate(invoice, customer, charges, branding);
    }
}
