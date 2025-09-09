// Datei: Components/Pages/Anja/Aufgabenseite.razor.cs
// Seite: Aufgabenseite (Code-Behind)

// Excel
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;
// PDF
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace ProActive2508.Components.Pages.Anja;

public partial class Aufgabenseite
{
    [Inject] private AppDbContext Db { get; set; } = default!;
    [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    protected bool isLoading = true;
    protected bool isModalOpen = false;
    protected string modalTitle = string.Empty;
    protected string? uiError = null;

    protected bool showDone = false;
    protected bool isProjektleiter = false;

    protected string searchMeine = string.Empty;
    protected string statusFilterMeine = "Alle";
    protected int selectedProjektMeine = -1;
    protected List<Projekt> projektFilterListMeine = new();

    protected string searchZuweisungen = string.Empty;
    protected string statusFilterZuweisungen = "Alle";
    protected int selectedProjektZuweisungen = -1;
    protected List<Projekt> projektFilterListZuweisungen = new();

    protected List<Aufgabe> meineAufgaben = new();
    protected List<Aufgabe> zuweisungenVonMir = new();

    protected Dictionary<int, string> projektLookup = new();
    protected Dictionary<int, string> benutzerLookup = new();

    protected Aufgabe editModel = new();
    protected List<Projekt> projekteAlsLeiter = new();
    protected List<Projekt> projektChoicesForModal = new();
    protected string projektSearch = string.Empty;

    protected List<Benutzer> benutzerChoices = new();
    protected List<Benutzer> benutzerChoicesForModal = new();
    protected string bearbeiterSearch = string.Empty;

    protected int CurrentUserId { get; private set; }
    protected string CurrentUserEmail { get; private set; } = string.Empty;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        uiError = null;

        try
        {
            var authState = await Auth.GetAuthenticationStateAsync();
            var user = authState.User;

            CurrentUserEmail = user.Identity?.Name ?? string.Empty;

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value
                          ?? user.FindFirst("userid")?.Value;

            if (!int.TryParse(idClaim, out var parsedId) || parsedId <= 0)
            {
                parsedId = await Db.Set<Benutzer>()
                    .Where(b => b.Email == CurrentUserEmail)
                    .Select(b => b.Id)
                    .FirstOrDefaultAsync();
            }
            CurrentUserId = parsedId;

            isProjektleiter = user.IsInRole("Projektleiter");

            if (!isProjektleiter)
            {
                try
                {
                    projekteAlsLeiter = await Db.Set<Projekt>()
                        .Where(p => p.ProjektleiterId == CurrentUserId)
                        .OrderBy(p => p.Projektbeschreibung)
                        .ToListAsync();

                    isProjektleiter = projekteAlsLeiter.Any();
                }
                catch
                {
                    projekteAlsLeiter = new();
                }
            }
            else
            {
                projekteAlsLeiter = await Db.Set<Projekt>()
                    .OrderBy(p => p.Projektbeschreibung)
                    .ToListAsync();
            }

            meineAufgaben = await Db.Set<Aufgabe>()
                .AsNoTracking()
                .Where(a => a.BenutzerId == CurrentUserId)
                .OrderBy(a => a.Faellig)
                .ToListAsync();

            if (isProjektleiter)
            {
                var q = Db.Set<Aufgabe>().AsNoTracking();

                if (HasProperty<Aufgabe>("ErstellerId"))
                    q = q.Where(a => EF.Property<int>(a, "ErstellerId") == CurrentUserId);
                else if (HasProperty<Aufgabe>("ZuweiserId"))
                    q = q.Where(a => EF.Property<int>(a, "ZuweiserId") == CurrentUserId);
                else
                {
                    var leitProjIds = projekteAlsLeiter.Select(p => p.Id).ToList();
                    q = q.Where(a => a.ProjektId.HasValue && leitProjIds.Contains(a.ProjektId.Value));
                }

                zuweisungenVonMir = await q.OrderBy(a => a.Faellig).ToListAsync();
            }
            else
            {
                zuweisungenVonMir.Clear();
            }

            var projektIds = meineAufgaben.Select(a => a.ProjektId)
                              .Concat(zuweisungenVonMir.Select(a => a.ProjektId))
                              .Where(id => id.HasValue && id.Value > 0)
                              .Select(id => id!.Value)
                              .Distinct()
                              .ToList();

            projektLookup = await Db.Set<Projekt>()
                .Where(p => projektIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Projektbeschreibung);

            var benutzerIds = meineAufgaben.Select(a => a.BenutzerId)
                               .Concat(zuweisungenVonMir.Select(a => a.BenutzerId))
                               .Distinct()
                               .ToList();

            var benutzerMails = await Db.Set<Benutzer>()
                .Where(b => benutzerIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Email })
                .ToListAsync();

            benutzerLookup = benutzerMails.ToDictionary(b => b.Id, b => b.Email ?? $"Id:{b.Id}");

            if (isProjektleiter)
            {
                projektFilterListMeine = projekteAlsLeiter.ToList();
                projektFilterListZuweisungen = projekteAlsLeiter.ToList();
            }
            else
            {
                projektFilterListMeine = await Db.Set<Projekt>()
                    .Where(p => projektIds.Contains(p.Id))
                    .OrderBy(p => p.Projektbeschreibung)
                    .ToListAsync();
                projektFilterListZuweisungen = new();
            }

            var keinProjekt = new Projekt { Id = 0, Projektbeschreibung = "(kein Projekt)" };
            if (isProjektleiter)
            {
                projektChoicesForModal = new List<Projekt> { keinProjekt };
                projektChoicesForModal.AddRange(projekteAlsLeiter);
            }
            else
            {
                var alle = await Db.Set<Projekt>()
                    .OrderBy(p => p.Projektbeschreibung)
                    .ToListAsync();
                projektChoicesForModal = new List<Projekt> { keinProjekt };
                projektChoicesForModal.AddRange(alle);
            }

            benutzerChoices = benutzerMails
                .Select(b => new Benutzer { Id = b.Id, Email = b.Email })
                .ToList();

            var me = benutzerChoices.FirstOrDefault(b => b.Id == CurrentUserId);
            if (me is null)
                benutzerChoices.Insert(0, new Benutzer { Id = CurrentUserId, Email = CurrentUserEmail });
            else
            {
                benutzerChoices.Remove(me);
                benutzerChoices.Insert(0, me);
            }

            if (isProjektleiter)
                benutzerChoicesForModal = benutzerChoices.ToList();
            else
                benutzerChoicesForModal = new List<Benutzer> { new() { Id = CurrentUserId, Email = CurrentUserEmail } };
        }
        catch (Exception ex)
        {
            uiError = "Fehler beim Laden der Seite: " + Short(ex);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    // ------- Filter-Ergebnisse, die im .razor verwendet werden (inkl. ZuweisungenGefiltert!) -------
    protected IEnumerable<Aufgabe> MeineGefiltert => ApplyFilters(meineAufgaben, searchMeine, statusFilterMeine, selectedProjektMeine);
    protected IEnumerable<Aufgabe> ZuweisungenGefiltert => ApplyFilters(zuweisungenVonMir, searchZuweisungen, statusFilterZuweisungen, selectedProjektZuweisungen);

    private IEnumerable<Aufgabe> ApplyFilters(IEnumerable<Aufgabe> src, string search, string statusFilter, int selectedProjektId)
    {
        IEnumerable<Aufgabe> q = src;

        if (selectedProjektId == 0)
            q = q.Where(a => !a.ProjektId.HasValue || a.ProjektId.Value == 0);
        else if (selectedProjektId > 0)
            q = q.Where(a => a.ProjektId == selectedProjektId);

        if (!showDone)
            q = q.Where(a => a.Erledigt != Erledigungsstatus.Erledigt);

        q = statusFilter switch
        {
            "Offen" => q.Where(a => a.Erledigt == Erledigungsstatus.Offen),
            "InBearbeitung" => q.Where(a => a.Erledigt == Erledigungsstatus.InBearbeitung),
            "Erledigt" => q.Where(a => a.Erledigt == Erledigungsstatus.Erledigt),
            _ => q
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(a =>
                (a.Aufgabenbeschreibung ?? string.Empty).ToLowerInvariant().Contains(term)
                || (benutzerLookup.TryGetValue(a.BenutzerId, out var mail)
                        ? (mail ?? string.Empty).ToLowerInvariant().Contains(term)
                        : false));
        }

        return q;
    }

    protected string GetProjektTextSafe(int? projektId)
        => !projektId.HasValue || projektId.Value <= 0
            ? string.Empty
            : (projektLookup.TryGetValue(projektId.Value, out var name) ? name : string.Empty);

    protected string GetProjektTextSafe(int projektId)
        => projektId <= 0 ? string.Empty
                          : (projektLookup.TryGetValue(projektId, out var name) ? name : string.Empty);

    protected void OpenCreateModal()
    {
        try
        {
            editModel = new Aufgabe
            {
                BenutzerId = CurrentUserId,
                ErstellVon = CurrentUserId,
                Faellig = DateTime.Today.AddDays(7),
                Erledigt = Erledigungsstatus.Offen,
                ProjektId = null
            };
            modalTitle = "Neue Aufgabe";
            isModalOpen = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            uiError = "Konnte Dialog nicht öffnen: " + Short(ex);
        }
    }

    protected async Task OpenEditModal(int id)
    {
        try
        {
            var found = await Db.Set<Aufgabe>().FirstOrDefaultAsync(a => a.Id == id);
            if (found is null) return;

            editModel = new Aufgabe
            {
                Id = found.Id,
                ProjektId = found.ProjektId,
                BenutzerId = found.BenutzerId,
                Aufgabenbeschreibung = found.Aufgabenbeschreibung,
                Faellig = found.Faellig,
                Phase = found.Phase,
                Erledigt = found.Erledigt,
                ErstellVon = found.ErstellVon
            };

            modalTitle = $"Aufgabe #{id} bearbeiten";
            isModalOpen = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            uiError = "Konnte Aufgabe nicht laden: " + Short(ex);
        }
    }

    protected void CloseModal() => isModalOpen = false;

    protected async Task SaveAsync()
    {
        uiError = null;

        if (string.IsNullOrWhiteSpace(editModel.Aufgabenbeschreibung))
        {
            uiError = "Bitte eine Aufgabenbeschreibung eingeben.";
            return;
        }

        if (editModel.ErstellVon <= 0) editModel.ErstellVon = CurrentUserId;

        // 🔒 Serverseitig erzwingen: Nur Projektleiter dürfen Aufgaben an andere zuweisen
        if (!isProjektleiter)
        {
            if (editModel.BenutzerId != CurrentUserId)
            {
                uiError = "Nur Projektleiter dürfen Aufgaben an andere zuweisen. Die Aufgabe wird dir selbst zugewiesen.";
            }
            editModel.BenutzerId = CurrentUserId;
        }

        try
        {
            if (editModel.Id == 0)
                Db.Set<Aufgabe>().Add(editModel);
            else
                Db.Set<Aufgabe>().Update(editModel);

            await Db.SaveChangesAsync();

            isModalOpen = false;
            await LoadAsync();
        }
        catch (DbUpdateException ex)
        {
            uiError = "Speichern fehlgeschlagen (DB-Fehler): " + Short(ex);
        }
        catch (Exception ex)
        {
            uiError = "Speichern fehlgeschlagen: " + Short(ex);
        }
    }

    protected async Task OnCheckboxChangedAsync(Aufgabe row, bool isChecked)
    {
        var neuerStatus = isChecked ? Erledigungsstatus.Erledigt : Erledigungsstatus.Offen;
        if (row.Erledigt == neuerStatus) return;

        var alterStatus = row.Erledigt;
        row.Erledigt = neuerStatus;
        StateHasChanged();

        try
        {
            var stub = new Aufgabe { Id = row.Id };
            Db.Attach(stub);
            Db.Entry(stub).Property(a => a.Erledigt).CurrentValue = neuerStatus;
            Db.Entry(stub).Property(a => a.Erledigt).IsModified = true;

            await Db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            row.Erledigt = alterStatus;
            uiError = "Konnte Status nicht speichern: " + Short(ex);
            StateHasChanged();
        }
    }

    protected async Task OnToggleShowDoneChanged(ChangeEventArgs e)
    {
        showDone = e?.Value is bool b && b;
        await Task.Yield();
    }

    protected void OnProjektPicked(Projekt? p)
    {
        editModel.ProjektId = (p == null || p.Id <= 0) ? (int?)null : p.Id;
    }

    protected void OnBearbeiterSelected(Benutzer? b)
    {
        // Nur Projektleiter dürfen die Zuordnung ändern
        if (!isProjektleiter) return;
        if (b is null) return;
        editModel.BenutzerId = b.Id;
    }

    protected string GetRowClass(Aufgabe a)
    {
        if (a.Erledigt == Erledigungsstatus.Erledigt) return string.Empty;
        var rest = (a.Faellig.Date - DateTime.Today).TotalDays;
        if (rest < 0) return "row--overdue";
        if (rest <= 2) return "row--soon";
        return string.Empty;
    }

    protected string GetStatusClass(Erledigungsstatus s) => s switch
    {
        Erledigungsstatus.Offen => "status-offen",
        Erledigungsstatus.InBearbeitung => "status-inbearbeitung",
        Erledigungsstatus.Erledigt => "status-erledigt",
        _ => "status-offen"
    };

    protected bool CanEdit(Aufgabe a) => isProjektleiter || a.BenutzerId == CurrentUserId;

    private static bool HasProperty<T>(string name) => typeof(T).GetProperty(name) != null;

    protected void ClearUiError() => uiError = null;

    private static string Short(Exception ex)
    {
        var e = ex.InnerException ?? ex;
        var msg = e.Message;
        return msg.Length > 400 ? msg[..400] + "…" : msg;
    }

    // -------- PDF: "Meine Aufgaben" exportieren --------
    protected async Task ExportMeineAufgabenPdfAsync()
    {
        var rows = MeineGefiltert.ToList();
        if (rows.Count == 0) return;

        var ci = new CultureInfo("de-DE");
        QuestPDF.Settings.License = LicenseType.Community;

        var pdfBytes = Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(d => d.FontSize(10));

                page.Header().Row(r =>
                {
                    r.RelativeItem().AlignLeft()
                        .Text(t => t.Span("PROACTIVE – Aufgaben").SemiBold().FontSize(12));
                    r.RelativeItem().AlignRight()
                        .Text(t => t.Span($"{DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9));
                });

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter()
                        .Text(t => t.Span($"MEINE AUFGABEN – {CurrentUserEmail}").SemiBold().FontSize(14));
                    col.Item().AlignCenter()
                        .Text(t => t.Span($"Stand: {DateTime.Today.ToString("dddd, dd.MM.yyyy", ci)}"));

                    col.Item().PaddingTop(6).Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(9));
                        t.Span("Filter: ").SemiBold();
                        t.Span($"Status={statusFilterMeine}, Projekt={(selectedProjektMeine switch { -1 => "Alle", 0 => "(kein Projekt)", _ => GetProjektTextSafe(selectedProjektMeine) })}, ");
                        t.Span($"Erledigte {(showDone ? "sichtbar" : "ausgeblendet")}");
                    });

                    col.Item().PaddingTop(10).Element(e =>
                    {
                        e.Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(40);   // Id
                                cols.RelativeColumn(2);    // Projekt
                                cols.RelativeColumn(4);    // Beschreibung
                                cols.ConstantColumn(70);   // Fällig
                                cols.ConstantColumn(45);   // Phase
                                cols.ConstantColumn(70);   // Status
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text(t => t.Span("Id").SemiBold());
                                h.Cell().Element(HeaderCell).Text(t => t.Span("Projekt").SemiBold());
                                h.Cell().Element(HeaderCell).Text(t => t.Span("Beschreibung").SemiBold());
                                h.Cell().Element(HeaderCell).Text(t => t.Span("Fällig").SemiBold());
                                h.Cell().Element(HeaderCell).Text(t => t.Span("Phase").SemiBold());
                                h.Cell().Element(HeaderCell).Text(t => t.Span("Status").SemiBold());
                            });

                            foreach (var a in rows)
                            {
                                table.Cell().Element(Cell).Text(a.Id.ToString());
                                table.Cell().Element(Cell).Text(GetProjektTextSafe(a.ProjektId));
                                table.Cell().Element(Cell).Text(a.Aufgabenbeschreibung ?? string.Empty);
                                table.Cell().Element(Cell).Text(a.Faellig.ToString("dd.MM.yyyy", ci));
                                table.Cell().Element(Cell).Text(a.Phase.ToString());
                                table.Cell().Element(Cell).Text(a.Erledigt.ToString());
                            }

                            static IContainer HeaderCell(IContainer c) =>
                                c.PaddingVertical(4).BorderBottom(1).DefaultTextStyle(s => s.FontSize(10));
                            static IContainer Cell(IContainer c) =>
                                c.PaddingVertical(2).DefaultTextStyle(s => s.FontSize(10));
                        });
                    });
                });

                page.Footer().AlignRight().Text(t => t.Span("ProActive").FontSize(8));
            });
        }).GeneratePdf();

        var fileName = $"MeineAufgaben_{DateTime.Today:yyyyMMdd}.pdf";
        var base64 = Convert.ToBase64String(pdfBytes);
        await JS.InvokeVoidAsync("downloadFileFromBytes", fileName, base64);
    }

    // -------- Excel: "Meine Aufgaben" exportieren (.xlsx via ClosedXML) --------
    protected async Task ExportMeineAufgabenExcelAsync()
    {
        var rows = MeineGefiltert.ToList();
        if (rows.Count == 0) return;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Meine Aufgaben");

        // Header
        ws.Cell(1, 1).Value = "Id";
        ws.Cell(1, 2).Value = "Projekt";
        ws.Cell(1, 3).Value = "Beschreibung";
        ws.Cell(1, 4).Value = "Fällig";
        ws.Cell(1, 5).Value = "Phase";
        ws.Cell(1, 6).Value = "Status";
        ws.Range(1, 1, 1, 6).Style.Font.Bold = true;

        // Rows
        var r = 2;
        foreach (var a in rows)
        {
            ws.Cell(r, 1).Value = a.Id;
            ws.Cell(r, 2).Value = GetProjektTextSafe(a.ProjektId);
            ws.Cell(r, 3).Value = a.Aufgabenbeschreibung ?? string.Empty;

            ws.Cell(r, 4).Value = a.Faellig;
            ws.Cell(r, 4).Style.DateFormat.Format = "dd.MM.yyyy";

            ws.Cell(r, 5).Value = a.Phase.ToString();
            ws.Cell(r, 6).Value = a.Erledigt.ToString();
            r++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var bytes = ms.ToArray();

        var fileName = $"MeineAufgaben_{DateTime.Today:yyyyMMdd}.xlsx";
        var base64 = Convert.ToBase64String(bytes);

        await JS.InvokeVoidAsync("downloadFileFromBytes", fileName, base64);
    }
}
