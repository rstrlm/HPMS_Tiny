using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pms.Application.DTOs;
using Pms.Application.Interfaces;

namespace Pms.Api.Controllers;

[ApiController]
[Route("api/v1/folios")]
[Authorize(Policy = "RequireFrontdesk")]
public class FoliosController : ControllerBase
{
    private readonly IFolioService _folioService;

    public FoliosController(IFolioService folioService)
    {
        _folioService = folioService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FolioDto>> GetById(Guid id)
    {
        var folio = await _folioService.GetByIdAsync(id);
        if (folio is null)
            return NotFound();

        return Ok(folio);
    }

    [HttpGet("by-customer/{customerId:guid}")]
    public async Task<ActionResult<IEnumerable<FolioSummaryDto>>> GetByCustomer(Guid customerId)
    {
        var folios = await _folioService.GetByCustomerAsync(customerId);
        return Ok(folios);
    }

    [HttpGet("by-reservation/{reservationId:guid}")]
    public async Task<ActionResult<FolioDto>> GetByReservation(Guid reservationId)
    {
        var folio = await _folioService.GetByReservationAsync(reservationId);
        if (folio is null)
            return NotFound();

        return Ok(folio);
    }

    [HttpPost]
    public async Task<ActionResult<FolioDto>> Create([FromBody] CreateFolioRequest request)
    {
        try
        {
            var folio = await _folioService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = folio.Id }, folio);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/charges")]
    public async Task<ActionResult<ChargeDto>> AddCharge(Guid id, [FromBody] CreateChargeRequest request)
    {
        try
        {
            var charge = await _folioService.AddChargeAsync(id, request);
            return Ok(charge);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("charges/{chargeId:guid}")]
    public async Task<IActionResult> RemoveCharge(Guid chargeId)
    {
        try
        {
            var result = await _folioService.RemoveChargeAsync(chargeId);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<PaymentDto>> AddPayment(Guid id, [FromBody] CreatePaymentRequest request)
    {
        try
        {
            var payment = await _folioService.AddPaymentAsync(id, request);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/issue-invoice")]
    public async Task<ActionResult<InvoiceDto>> IssueInvoice(Guid id)
    {
        try
        {
            var invoice = await _folioService.IssueInvoiceAsync(id);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/invoices")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoices(Guid id)
    {
        var invoices = await _folioService.GetInvoicesByFolioAsync(id);
        return Ok(invoices);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<FolioDto>> CloseFolio(Guid id)
    {
        try
        {
            var folio = await _folioService.CloseFolioAsync(id);
            if (folio is null)
                return NotFound();

            return Ok(folio);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("invoices/{invoiceId:guid}/void")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<InvoiceDto>> VoidInvoice(Guid invoiceId)
    {
        try
        {
            var invoice = await _folioService.VoidInvoiceAsync(invoiceId);
            if (invoice is null)
                return NotFound();

            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<FolioDto>> CancelFolio(Guid id)
    {
        try
        {
            var folio = await _folioService.CancelFolioAsync(id);
            if (folio is null)
                return NotFound();

            return Ok(folio);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("merge")]
    [Authorize(Policy = "RequireManager")]
    public async Task<ActionResult<FolioDto>> MergeFolios([FromBody] MergeFoliosRequest request)
    {
        try
        {
            var folio = await _folioService.MergeFoliosAsync(request.TargetFolioId, request.SourceFolioIds);
            return Ok(folio);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("invoices/{invoiceId:guid}/pdf")]
    public async Task<IActionResult> DownloadInvoicePdf(Guid invoiceId)
    {
        try
        {
            var pdfBytes = await _folioService.GenerateInvoicePdfAsync(invoiceId);
            return File(pdfBytes, "application/pdf", $"invoice-{invoiceId}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
