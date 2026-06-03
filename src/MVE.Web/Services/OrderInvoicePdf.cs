using Application.Orders.Queries.GetMyOrderById;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Web.Services;

public static class OrderInvoicePdf
{
    public static byte[] Generate(MyOrderDetailDto o)
    {
        return Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Ramadan Abdullah").Bold().FontSize(18);
                        col.Item().Text("Invoice").FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(180).AlignRight().Column(col =>
                    {
                        col.Item().Text(o.OrderNumber).Bold().FontSize(12);
                        col.Item().Text($"Placed: {(o.PlacedAtUtc?.ToString("yyyy-MM-dd HH:mm") ?? "—")}").FontColor(Colors.Grey.Medium);
                        col.Item().Text($"Status: {o.Status}").FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c2 =>
                        {
                            c2.Item().Text("Ship to").Bold();
                            c2.Item().Text(o.ShippingFullName ?? "—");
                            c2.Item().Text(o.ShippingLine1 ?? "");
                            if (!string.IsNullOrEmpty(o.ShippingLine2)) c2.Item().Text(o.ShippingLine2);
                            c2.Item().Text($"{o.ShippingCity}{(string.IsNullOrEmpty(o.ShippingState) ? "" : $", {o.ShippingState}")} {o.ShippingPostalCode}");
                            c2.Item().Text(o.ShippingCountry ?? "");
                            if (!string.IsNullOrEmpty(o.ShippingPhone)) c2.Item().Text(o.ShippingPhone).FontColor(Colors.Grey.Medium);
                        });
                        r.ConstantItem(180).AlignRight().Column(c2 =>
                        {
                            c2.Item().Text("Payment").Bold();
                            c2.Item().Text($"Method: {o.PaymentMethod ?? "—"}");
                            c2.Item().Text($"Status: {o.PaymentStatus}");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(4);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("Product");
                            h.Cell().Element(HeaderCell).Text("Vendor");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Unit");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Total");
                        });
                        foreach (var i in o.Items)
                        {
                            table.Cell().Element(BodyCell).Text(i.ProductName + (string.IsNullOrEmpty(i.VariantName) ? "" : $" — {i.VariantName}"));
                            table.Cell().Element(BodyCell).Text(i.StoreName);
                            table.Cell().Element(BodyCell).AlignRight().Text(i.Quantity.ToString());
                            table.Cell().Element(BodyCell).AlignRight().Text(i.UnitPrice.ToString("0.00"));
                            table.Cell().Element(BodyCell).AlignRight().Text(i.LineTotal.ToString("0.00"));
                        }
                    });

                    col.Item().AlignRight().Width(200).Column(c2 =>
                    {
                        TotalsRow(c2, "Subtotal", o.Subtotal);
                        if (o.ShippingAmount > 0) TotalsRow(c2, "Shipping", o.ShippingAmount);
                        if (o.TaxAmount > 0)      TotalsRow(c2, "Tax",       o.TaxAmount);
                        if (o.DiscountAmount > 0) TotalsRow(c2, "Discount", -o.DiscountAmount);
                        c2.Item().BorderTop(1).BorderColor(Colors.Grey.Medium).PaddingTop(4);
                        c2.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total").Bold();
                            r.ConstantItem(80).AlignRight().Text(o.Total.ToString("0.00")).Bold();
                        });
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Ramadan Abdullah — generated ").FontColor(Colors.Grey.Medium);
                    t.Span(DateTime.UtcNow.ToString("u")).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer c) => c.Background(Colors.Grey.Lighten3).PaddingVertical(5).PaddingHorizontal(6).DefaultTextStyle(t => t.SemiBold());
    private static IContainer BodyCell(IContainer c)   => c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(6);

    private static void TotalsRow(ColumnDescriptor c, string label, decimal value) =>
        c.Item().Row(r =>
        {
            r.RelativeItem().Text(label);
            r.ConstantItem(80).AlignRight().Text(value.ToString("0.00"));
        });
}
