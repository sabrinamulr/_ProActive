// Datei: Service/MenuplanPdfService.cs
// Seite: MenuplanPdfService

using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProActive2508.Service
{
    public sealed class MenuplanPdfService : IMenuplanPdfService
    {
        private static readonly CultureInfo Ci = new("de-DE");

        public Task<byte[]> BuildMenuplanPdfAsync(
            DateTime monday, DateTime friday,
            string? kantinenName,
            string title,
            IEnumerable<PdfMenuDay> days,
            decimal? mealPrice,
            string? logoPath = null)
        {
            var items = days.OrderBy(d => d.Date).ToList();
            var priceLine = mealPrice.HasValue ? $"Menü I oder II € {mealPrice.Value:0.00}" : "Menü I oder II";

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Kopf
                    page.Header().Element(h =>
                    {
                        h.Row(row =>
                        {
                            row.RelativeItem()
                               .AlignLeft()
                               .Text(t => t.Span(kantinenName ?? string.Empty).SemiBold().FontSize(14));

                            row.ConstantItem(120)
                               .AlignRight()
                               .Text(t => t.Span("MAGNA").SemiBold().FontSize(18));
                        });
                    });

                    // Inhalt
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().AlignCenter()
                           .Text(t => t.Span($"{title} {monday:dd.MM.yyyy} – {friday:dd.MM.yyyy}")
                                     .SemiBold().FontSize(16));

                        col.Item().PaddingTop(4).AlignCenter()
                           .Text(t => t.Span(priceLine).Underline());

                        col.Item().PaddingTop(12).Column(daysCol =>
                        {
                            foreach (var d in items)
                                daysCol.Item().Element(e => RenderDay(e, d));
                        });

                        // Info-Kästchen (FontSize IM Lambda!)
                        col.Item().PaddingTop(12).Row(r =>
                        {
                            r.RelativeItem().Background("#eeeeee").Padding(8).Text(t =>
                            {
                                t.DefaultTextStyle(x => x.FontSize(9));
                                t.Span("* Tagessuppe nach Marktangebot. ");
                                t.Span("Nach Absprache auch vegane Varianten des Menüs möglich.");
                            });

                            r.RelativeItem().Background("#eeeeee").Padding(8).Text(t =>
                            {
                                t.DefaultTextStyle(x => x.FontSize(9));
                                t.Span("Die Küche legt großen Wert auf Qualität und regionale Produkte. Das angebotene Fleisch stammt aus Österreich.");
                            });

                            r.RelativeItem().Background("#eeeeee").Padding(8).Text(t =>
                            {
                                t.DefaultTextStyle(x => x.FontSize(9));
                                t.Span("GUTEN APPETIT wünscht das Kantinenteam!");
                            });
                        });

                        // Allergene-Legende (FontSize IM Lambda!)
                        col.Item().PaddingTop(12).Element(RenderAllergenLegend);
                    });

                    page.Footer().AlignRight().Text(t => t.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8));
                });
            });

            var bytes = doc.GeneratePdf();
            return Task.FromResult(bytes);
        }

        private static void RenderDay(IContainer c, PdfMenuDay d)
        {
            c.Column(col =>
            {
                col.Item().Text(t => t.Span(d.Date.ToString("dddd", Ci).ToUpperInvariant())
                                     .SemiBold().FontSize(12));
                col.Item().Text(t => t.Span("Tagessuppe*").Italic());

                if (!string.IsNullOrWhiteSpace(d.Menu1))
                    col.Item().Text(t =>
                    {
                        t.Span("I: ").SemiBold();
                        t.Span(d.Menu1);
                        var a = Clean(d.Menu1Allergens);
                        if (!string.IsNullOrWhiteSpace(a))
                            t.Span($".  {a}").FontSize(10);
                    });

                if (!string.IsNullOrWhiteSpace(d.Menu2))
                    col.Item().Text(t =>
                    {
                        t.Span("II: ").SemiBold();
                        t.Span(d.Menu2);
                        var a = Clean(d.Menu2Allergens);
                        if (!string.IsNullOrWhiteSpace(a))
                            t.Span($".  {a}").FontSize(10);
                    });

                col.Item().PaddingBottom(6);
            });

            static string Clean(string s) =>
                (s ?? string.Empty).Trim().TrimEnd(',').Replace(", ", ", ").ToUpperInvariant();
        }

        private static void RenderAllergenLegend(IContainer c)
        {
            var legend = new (string Code, string Text)[]
            {
                ("A","Glutenhaltiges Getreide"), ("B","Krebstiere"), ("C","Eier"),
                ("D","Fischerzeugnisse"), ("E","Erdnüsse"), ("F","Soja"),
                ("G","Milch"), ("H","Schalenfrüchte"), ("L","Sellerie"), ("M","Senf"),
                ("N","Sesam"), ("O","Schwefeldioxid"), ("P","Lupinen"), ("R","Weichtiere")
            };

            c.Column(col =>
            {
                col.Item().Text(t => t.Span("Allergene:").SemiBold());
                col.Item().Row(r =>
                {
                    foreach (var (code, text) in legend)
                        r.RelativeItem().Text(t => t.Span($"{code}: {text}").FontSize(9));
                });
            });
        }
    }
}
