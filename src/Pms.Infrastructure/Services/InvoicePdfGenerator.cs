using Pms.Application.Settings;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Pms.Infrastructure.Services;

public static class InvoicePdfGenerator
{
    static InvoicePdfGenerator()
    {
        // QuestPDF Community license for open source projects
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(Invoice invoice, Customer customer, List<Charge> charges, BrandingSettings branding)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, invoice, branding));
                page.Content().Element(c => ComposeContent(c, invoice, customer, charges, branding));
                page.Footer().Element(c => ComposeFooter(c, branding));
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Invoice invoice, BrandingSettings branding)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(branding.CompanyName)
                        .FontSize(24).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text(branding.Tagline)
                        .FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("INVOICE")
                        .FontSize(20).Bold().FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"#{invoice.InvoiceNumber}")
                        .FontSize(14).FontColor(Colors.Grey.Darken1);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice, Customer customer, List<Charge> charges, BrandingSettings branding)
    {
        container.Column(column =>
        {
            // Customer and Invoice Info
            column.Item().PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Bill To:").Bold();
                    col.Item().Text(customer.Name);
                    if (!string.IsNullOrEmpty(customer.Address))
                        col.Item().Text(customer.Address);
                    if (!string.IsNullOrEmpty(customer.Email))
                        col.Item().Text(customer.Email);
                    if (!string.IsNullOrEmpty(customer.Phone))
                        col.Item().Text(customer.Phone);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Invoice Date: {invoice.IssuedAtUtc:dd.MM.yyyy}");
                    col.Item().Text($"Status: {(invoice.Status == InvoiceStatus.Issued ? "Issued" : "Voided")}");
                });
            });

            // Charges Table
            column.Item().Element(c => ComposeChargesTable(c, charges));

            // Totals
            column.Item().PaddingTop(20).AlignRight().Width(200).Column(totals =>
            {
                totals.Item().Row(row =>
                {
                    row.RelativeItem().Text("Subtotal:");
                    row.ConstantItem(80).AlignRight().Text($"{invoice.SubTotal:N2} €");
                });

                totals.Item().Row(row =>
                {
                    row.RelativeItem().Text("VAT:");
                    row.ConstantItem(80).AlignRight().Text($"{invoice.VatTotal:N2} €");
                });

                totals.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
                {
                    row.RelativeItem().Text("Total:").Bold();
                    row.ConstantItem(80).AlignRight().Text($"{invoice.GrandTotal:N2} €").Bold();
                });
            });

            // Payment Info
            column.Item().PaddingTop(30).Column(paymentInfo =>
            {
                paymentInfo.Item().Text("Payment Information").Bold();
                paymentInfo.Item().Text($"Bank: {branding.BankName}");
                paymentInfo.Item().Text($"IBAN: {branding.IBAN}");
                paymentInfo.Item().Text($"BIC: {branding.BIC}");
                paymentInfo.Item().Text($"Reference: {invoice.InvoiceNumber}");
            });
        });
    }

    private static void ComposeChargesTable(IContainer container, List<Charge> charges)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Description
                columns.ConstantColumn(50); // Qty
                columns.ConstantColumn(70); // Unit Price
                columns.ConstantColumn(50); // VAT %
                columns.ConstantColumn(80); // Total
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Description").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Qty").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Unit Price").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("VAT %").Bold();
                header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
            });

            // Rows
            foreach (var charge in charges)
            {
                var chargeTypeLabel = charge.Type switch
                {
                    ChargeType.RoomNight => "Accommodation",
                    ChargeType.Treatment => "Spa Treatment",
                    ChargeType.Custom => "Other",
                    _ => "Other"
                };

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                    .Column(col =>
                    {
                        col.Item().Text(charge.Description);
                        col.Item().Text(chargeTypeLabel).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter()
                    .Text(charge.Quantity.ToString());
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                    .Text($"{charge.UnitPrice:N2} €");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter()
                    .Text($"{charge.VatRate * 100:N0}%");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                    .Text($"{charge.Total:N2} €");
            }
        });
    }

    private static void ComposeFooter(IContainer container, BrandingSettings branding)
    {
        container.Column(column =>
        {
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(branding.CompanyLegalName).FontSize(8);
                    col.Item().Text(branding.Address).FontSize(8);
                    col.Item().Text($"Y-tunnus: {branding.TaxId}").FontSize(8);
                });

                row.RelativeItem().AlignCenter().Column(col =>
                {
                    col.Item().Text(branding.Email).FontSize(8);
                    col.Item().Text(branding.Phone).FontSize(8);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text(text =>
                    {
                        text.Span("Page ").FontSize(8);
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" of ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
                });
            });
        });
    }
}
