using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Services;

public interface IInvoicePdfService
{
    byte[] GenerateInvoicePdf(CustomerInvoice invoice);
}

public class InvoicePdfService : IInvoicePdfService
{
    public InvoicePdfService()
    {
        // Set QuestPDF license (Community license for non-commercial use)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(CustomerInvoice invoice)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(container => ComposeContent(container, invoice));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Shiv Furniture ERP").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                column.Item().Text("Invoice").FontSize(14).Bold();
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text($"Date: {DateTime.Now:dd MMM yyyy}").FontSize(9);
            });
        });
    }

    private void ComposeContent(IContainer container, CustomerInvoice invoice)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(10);

            // Invoice Info Section
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Bill To:").Bold();
                    col.Item().Text(invoice.Customer?.Name ?? "Unknown Customer");
                    if (!string.IsNullOrWhiteSpace(invoice.Customer?.Email))
                    {
                        col.Item().Text(invoice.Customer.Email).FontSize(9);
                    }
                    if (!string.IsNullOrWhiteSpace(invoice.Customer?.Phone))
                    {
                        col.Item().Text(invoice.Customer.Phone).FontSize(9);
                    }
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Invoice #: {invoice.InvoiceNumber}").Bold();
                    col.Item().Text($"Invoice Date: {invoice.InvoiceDate:dd MMM yyyy}");
                    col.Item().Text($"Due Date: {invoice.DueDate:dd MMM yyyy}");
                    col.Item().Text($"Status: {invoice.Status}");
                });
            });

            column.Item().PaddingTop(20);

            // Invoice Lines Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Product").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Qty").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Unit Price").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();
                });

                // Items
                foreach (var line in invoice.Lines.OrderBy(l => l.CustomerInvoiceLineId))
                {
                    table.Cell().Element(CellStyle).Text(line.Product?.Name ?? $"Product #{line.ProductId}");
                    table.Cell().Element(CellStyle).AlignRight().Text(line.Quantity.ToString("N2"));
                    table.Cell().Element(CellStyle).AlignRight().Text($"?{line.UnitPrice:N2}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"?{line.Total:N2}");
                }

                // Footer - Totals
                table.Footer(footer =>
                {
                    footer.Cell().ColumnSpan(3).Element(CellStyle).AlignRight().Text("Subtotal:").Bold();
                    footer.Cell().Element(CellStyle).AlignRight().Text($"?{invoice.TotalAmount:N2}").Bold();

                    footer.Cell().ColumnSpan(3).Element(CellStyle).AlignRight().Text("Amount Paid:").Bold().FontColor(Colors.Green.Medium);
                    footer.Cell().Element(CellStyle).AlignRight().Text($"?{invoice.AmountPaid:N2}").Bold().FontColor(Colors.Green.Medium);

                    var amountDue = invoice.TotalAmount - invoice.AmountPaid;
                    footer.Cell().ColumnSpan(3).Element(CellStyle).AlignRight().Text("Amount Due:").Bold().FontColor(Colors.Red.Medium);
                    footer.Cell().Element(CellStyle).AlignRight().Text($"?{amountDue:N2}").Bold().FontColor(Colors.Red.Medium);
                });
            });

            // Payment History
            if (invoice.Payments.Any())
            {
                column.Item().PaddingTop(20).Text("Payment History").Bold().FontSize(12);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Payment #").Bold();
                        header.Cell().Element(CellStyle).Text("Date").Bold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Amount").Bold();
                        header.Cell().Element(CellStyle).Text("Mode").Bold();
                    });

                    foreach (var payment in invoice.Payments.OrderByDescending(p => p.PaymentDate))
                    {
                        table.Cell().Element(CellStyle).Text(payment.PaymentNumber);
                        table.Cell().Element(CellStyle).Text(payment.PaymentDate.ToString("dd MMM yyyy"));
                        table.Cell().Element(CellStyle).AlignRight().Text($"?{payment.Amount:N2}");
                        table.Cell().Element(CellStyle).Text(payment.PaymentMode.ToString());
                    }
                });
            }

            // Footer note
            column.Item().PaddingTop(20).Text("Thank you for your business!").FontSize(9).Italic();
        });
    }

    private IContainer CellStyle(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
    }
}
