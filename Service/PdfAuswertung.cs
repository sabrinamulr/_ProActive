using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using static ProActive2508.Components.Pages.Anja.KantinenTeil.Auswertung;

public class PdfAuswertung
{
    public byte[] BuildKochAuswertungPdf(
        string preisVerlaufImg,
        string preisNachfrageImg,
        string beliebtesteImg,
        string nachfrageWocheImg,
        string allergeneImg,
        List<PreisNachfrageItem> preisNachfrage,
        List<BeliebtheitItem> beliebteste,
        List<NachfrageWocheItem> nachfrageWoche,
        List<AllergenVerteilungItem> allergene
    )
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header().Text("Auswertung Küche")
                    .FontSize(22)
                    .Bold()
                    .AlignCenter();

                page.Content().Column(col =>
                {
                    // ⭐ Diagramm 1
                    col.Item().Text("Preisverlauf").FontSize(16).Bold();
                    col.Item().Image(Base64ToBytes(preisVerlaufImg));

                    // ⭐ Diagramm 2
                    col.Item().Text("Preis vs Nachfrage").FontSize(16).Bold();
                    col.Item().Image(Base64ToBytes(preisNachfrageImg));

                    // ⭐ Diagramm 3
                    col.Item().Text("Beliebteste Gerichte").FontSize(16).Bold();
                    col.Item().Image(Base64ToBytes(beliebtesteImg));

                    // ⭐ Diagramm 4
                    col.Item().Text("Nachfrage pro Woche").FontSize(16).Bold();
                    col.Item().Image(Base64ToBytes(nachfrageWocheImg));

                    // ⭐ Diagramm 5
                    col.Item().Text("Allergene").FontSize(16).Bold();
                    col.Item().Image(Base64ToBytes(allergeneImg));

                    // ⭐ Tabelle: Preis vs Nachfrage
                    col.Item().PaddingTop(20).Text("Preis vs Nachfrage – Tabelle").FontSize(16).Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(80);
                            c.ConstantColumn(80);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Gericht").Bold();
                            h.Cell().Text("Preis").Bold();
                            h.Cell().Text("Nachfrage").Bold();
                        });

                        foreach (var row in preisNachfrage)
                        {
                            t.Cell().Text(row.GerichtName);
                            t.Cell().Text($"{row.Preis:0.00} €");
                            t.Cell().Text(row.Nachfrage.ToString());
                        }
                    });

                    // ⭐ Tabelle: Beliebteste Gerichte
                    col.Item().PaddingTop(20).Text("Beliebteste Gerichte – Tabelle").FontSize(16).Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(80);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Gericht").Bold();
                            h.Cell().Text("Anzahl").Bold();
                        });

                        foreach (var row in beliebteste)
                        {
                            t.Cell().Text(row.GerichtName);
                            t.Cell().Text(row.Anzahl.ToString());
                        }
                    });

                    // ⭐ Tabelle: Nachfrage pro Woche
                    col.Item().PaddingTop(20).Text("Nachfrage pro Woche – Tabelle").FontSize(16).Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(80);
                            c.ConstantColumn(80);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("KW").Bold();
                            h.Cell().Text("Anzahl").Bold();
                        });

                        foreach (var row in nachfrageWoche)
                        {
                            t.Cell().Text(row.Woche.ToString());
                            t.Cell().Text(row.Anzahl.ToString());
                        }
                    });

                    // ⭐ Tabelle: Allergene
                    col.Item().PaddingTop(20).Text("Allergene – Tabelle").FontSize(16).Bold();
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(80);
                        });

                        t.Header(h =>
                        {
                            h.Cell().Text("Allergen").Bold();
                            h.Cell().Text("Anzahl").Bold();
                        });

                        foreach (var row in allergene)
                        {
                            t.Cell().Text(row.Name);
                            t.Cell().Text(row.Anzahl.ToString());
                        }
                    });
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Erstellt am ").FontSize(10);
                    txt.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontSize(10).Bold();
                });
            });
        }).GeneratePdf();
    }

    private byte[] Base64ToBytes(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return Array.Empty<byte>();

        return Convert.FromBase64String(
            base64.Replace("data:image/png;base64,", "")
        );
    }
}