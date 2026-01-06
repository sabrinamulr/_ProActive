using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Components.Pages.Sabrina
{
    public partial class ProjekteAnzeigen : ComponentBase
    {
        protected List<Projekt>? projects;
        protected Dictionary<int, string> userLookup = new();
        protected bool isLoading = true;
        protected string? uiError;

        protected bool isModalOpen = false;
        protected string modalTitle = string.Empty;
        protected Projekt? editModel;

        // Phasen-Lookups
        protected Dictionary<int, List<ProjektPhase>> projectPhasesLookup = new();
        protected Dictionary<int, ProjektPhase?> currentPhaseLookup = new();
        protected bool isProjektleiterRole = false;

        // für Modal-Edit: Phase-Selections + Benutzerliste
        protected List<PhaseEditConfig> editPhaseSelections = new();
        protected List<Benutzer> allUsers = new();

        [Inject] private AppDbContext Db { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        private int CurrentUserId;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            uiError = null;

            try
            {
                AuthenticationState auth = await AuthenticationStateTask;
                System.Security.Claims.ClaimsPrincipal user = auth.User;

                isProjektleiterRole = user.IsInRole("Projektleiter");

                string? idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                  ?? user.FindFirst("sub")?.Value;
                int parsed = 0;
                if (!int.TryParse(idClaim, out parsed) || parsed <= 0)
                {
                    string? name = user.Identity?.Name ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name) && int.TryParse(name, out int pn))
                    {
                        Benutzer? dbUser = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == pn);
                        parsed = dbUser?.Id ?? 0;
                    }
                    else
                    {
                        Benutzer? dbUserByMail = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Email == name);
                        parsed = dbUserByMail?.Id ?? 0;
                    }
                }
                CurrentUserId = parsed;

                // --- Lade Projekte: nur beteiligte Benutzer sehen Projekte (Projektleiter rollen optional: gesamte Übersicht) ---
                List<int> memberProjectIds = await Db.ProjektBenutzer
                    .AsNoTracking()
                    .Where(pb => pb.BenutzerId == CurrentUserId)
                    .Select(pb => pb.ProjektId)
                    .ToListAsync();

                if (isProjektleiterRole)
                {
                    // Projektleiter bekommen komplette Übersicht; falls gewünscht, hier einschränken auf eigene Projekte ändern
                    projects = await Db.Projekte
                        .AsNoTracking()
                        .OrderBy(p => p.Id)
                        .ToListAsync();
                }
                else
                {
                    projects = await Db.Projekte
                        .AsNoTracking()
                        .Where(p => p.ProjektleiterId == CurrentUserId
                                 || p.AuftraggeberId == CurrentUserId
                                 || memberProjectIds.Contains(p.Id))
                        .OrderBy(p => p.Id)
                        .ToListAsync();
                }

                // --- restlicher Code unverändert (userLookup, Phasen laden etc.) ---
                List<int> userIds = projects.SelectMany(p => new[] { p.ProjektleiterId, p.AuftraggeberId })
                                      .Distinct()
                                      .Where(id => id > 0)
                                      .ToList();

                if (userIds.Any())
                {
                    userLookup = await Db.Benutzer
                        .AsNoTracking()
                        .Where(b => userIds.Contains(b.Id))
                        .ToDictionaryAsync(b => b.Id, b => string.IsNullOrWhiteSpace(b.Email) ? $"User#{b.Id}" : b.Email);
                }
                else
                {
                    userLookup = new Dictionary<int, string>();
                }

                // --- Lade Phasen für angezeigte Projekte ---
                List<int> projectIds = projects.Select(p => p.Id).ToList();
                if (projectIds.Any())
                {
                    List<ProjektPhase> phases = await Db.ProjektPhasen
                        .AsNoTracking()
                        .Where(pp => projectIds.Contains(pp.ProjekteId))
                        .Include(pp => pp.Phase)
                        .OrderBy(pp => pp.StartDate)
                        .ToListAsync();

                    projectPhasesLookup = phases
                        .GroupBy(pp => pp.ProjekteId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // bestimme aktuelle Phase je Projekt
                    foreach (KeyValuePair<int, List<ProjektPhase>> kv in projectPhasesLookup)
                    {
                        List<ProjektPhase> list = kv.Value;
                        ProjektPhase? current = null;
                        DateTime today = DateTime.Today;

                        current = list.FirstOrDefault(pp =>
                            pp.StartDate.Date <= today && pp.DueDate.Date >= today && (pp.Abschlussdatum == null || pp.Abschlussdatum.Value.Date >= today));

                        if (current == null)
                            current = list.Where(pp => pp.StartDate.Date <= today).OrderByDescending(pp => pp.StartDate).FirstOrDefault();

                        if (current == null)
                            current = list.OrderBy(pp => pp.StartDate).FirstOrDefault();

                        currentPhaseLookup[kv.Key] = current;
                    }
                }
                else
                {
                    projectPhasesLookup = new Dictionary<int, List<ProjektPhase>>();
                }
            }
            catch (Exception ex)
            {
                uiError = "Fehler beim Laden: " + (ex.InnerException?.Message ?? ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        protected bool CanEdit(Projekt p)
        {
            return isProjektleiterRole || p.ProjektleiterId == CurrentUserId;
        }

        protected async Task OpenEditModal(int projektId)
        {
            editModel = projects?.FirstOrDefault(p => p.Id == projektId);
            if (editModel is null) { uiError = "Projekt nicht gefunden."; return; }

            modalTitle = $"Projekt #{projektId} bearbeiten";

            // lade Benutzer für Verantwortlichen-Auswahl
            allUsers = await Db.Benutzer.AsNoTracking().OrderBy(b => b.Email).ToListAsync();

            // lade vorhandene Projektphasen (inkl. Phase navigation) – wenn keine vorhanden: biete erste globale Phase an
            List<ProjektPhase> existing = await Db.ProjektPhasen
                .AsNoTracking()
                .Where(pp => pp.ProjekteId == projektId)
                .Include(pp => pp.Phase)
                .OrderBy(pp => pp.StartDate)
                .ToListAsync();

            if (!existing.Any())
            {
                Phase? first = await Db.Phasen.AsNoTracking().OrderBy(p => p.Id).FirstOrDefaultAsync();
                if (first != null)
                {
                    editPhaseSelections = new List<PhaseEditConfig>
                    {
                        new PhaseEditConfig
                        {
                            ExistingId = 0,
                            PhasenId = first.Id,
                            PhaseKurz = first.Kurzbezeichnung,
                            StartDate = DateTime.Today,
                            DueDate = DateTime.Today.AddDays(14),
                            VerantwortlicherbenutzerId = editModel.ProjektleiterId,
                            Status = "Geplant",
                            // Berechtigung: Projekleiter-Rolle oder der Verantwortliche selbst darf bearbeiten
                            CanEdit = isProjektleiterRole || editModel.ProjektleiterId == CurrentUserId
                        }
                    };
                }
                else
                {
                    editPhaseSelections = new List<PhaseEditConfig>();
                }
            }
            else
            {
                editPhaseSelections = existing.Select(pp => new PhaseEditConfig
                {
                    ExistingId = pp.Id,
                    PhasenId = pp.PhasenId,
                    PhaseKurz = pp.Phase?.Kurzbezeichnung ?? $"Phase {pp.PhasenId}",
                    StartDate = pp.StartDate,
                    DueDate = pp.DueDate,
                    VerantwortlicherbenutzerId = pp.VerantwortlicherbenutzerId,
                    Status = pp.Status,
                    Notizen = pp.Notizen,
                    // Berechtigung: Projekleiter-Rolle oder der für diese Phase Zuständige darf bearbeiten
                    CanEdit = isProjektleiterRole || pp.VerantwortlicherbenutzerId == CurrentUserId || editModel.ProjektleiterId == CurrentUserId
                }).ToList();
            }

            isModalOpen = true;
        }

        protected void CloseModal()
        {
            isModalOpen = false;
            editModel = null;
            editPhaseSelections = new List<PhaseEditConfig>();
            allUsers = new List<Benutzer>();
        }

        protected async Task SaveEditAsync()
        {
            uiError = null;
            if (editModel is null) return;

            if (!CanEdit(editModel))
            {
                uiError = "Keine Berechtigung zum Bearbeiten dieses Projekts.";
                return;
            }

            // Validierung Phase-Daten
            foreach (PhaseEditConfig cfg in editPhaseSelections)
            {
                if (cfg.StartDate.Date > cfg.DueDate.Date)
                {
                    uiError = $"Fehler: Für Phase '{cfg.PhaseKurz}' ist Startdatum nach dem Due‑Datum.";
                    return;
                }
                if (!string.IsNullOrEmpty(cfg.Notizen) && cfg.Notizen.Length > 2000)
                {
                    uiError = $"Fehler: Notizen für Phase '{cfg.PhaseKurz}' überschreiten {PhasenDefinieren.PhaseConfig.MaxNotesLength} Zeichen.";
                    return;
                }
            }

            try
            {
                // Update Projekt
                Db.Projekte.Update(editModel);

                // Upsert der ProjektPhasen
                List<ProjektPhase> existing = await Db.ProjektPhasen.Where(pp => pp.ProjekteId == editModel.Id).ToListAsync();

                foreach (PhaseEditConfig sel in editPhaseSelections)
                {
                    // Server-side Berechtigungscheck: nur Projekleiter-Rolle oder Phasen-Verantwortlicher darf Änderungen durchführen
                    if (!isProjektleiterRole)
                    {
                        if (sel.ExistingId != 0)
                        {
                            ProjektPhase? dbPhase = existing.FirstOrDefault(e => e.Id == sel.ExistingId);
                            if (dbPhase == null || dbPhase.VerantwortlicherbenutzerId != CurrentUserId)
                            {
                                uiError = $"Keine Berechtigung, Phase '{sel.PhaseKurz}' zu ändern.";
                                return;
                            }
                        }
                        else
                        {
                            // neue Phase: nur erlauben, wenn der aktuelle Benutzer der Verantwortliche ist
                            if (sel.VerantwortlicherbenutzerId != CurrentUserId && editModel.ProjektleiterId != CurrentUserId)
                            {
                                uiError = $"Keine Berechtigung, neue Phase '{sel.PhaseKurz}' anzulegen.";
                                return;
                            }
                        }
                    }

                    if (sel.ExistingId != 0)
                    {
                        ProjektPhase? upd = existing.FirstOrDefault(e => e.Id == sel.ExistingId);
                        if (upd != null)
                        {
                            upd.StartDate = sel.StartDate;
                            upd.DueDate = sel.DueDate;
                            upd.VerantwortlicherbenutzerId = sel.VerantwortlicherbenutzerId;
                            upd.Notizen = sel.Notizen;
                            upd.Status = sel.Status;
                            Db.ProjektPhasen.Update(upd);
                        }
                    }
                    else
                    {
                        ProjektPhase neu = new ProjektPhase
                        {
                            ProjekteId = editModel.Id,
                            PhasenId = sel.PhasenId,
                            StartDate = sel.StartDate,
                            DueDate = sel.DueDate,
                            VerantwortlicherbenutzerId = sel.VerantwortlicherbenutzerId,
                            Status = sel.Status,
                            Notizen = sel.Notizen
                        };
                        Db.ProjektPhasen.Add(neu);
                    }
                }

                await Db.SaveChangesAsync();

                isModalOpen = false;
                editModel = null;
                editPhaseSelections.Clear();
                await OnInitializedAsync(); // reload projektliste & lookups
            }
            catch (Exception ex)
            {
                uiError = "Fehler beim Speichern: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }

        protected class PhaseEditConfig
        {
            public int ExistingId { get; set; }
            public int PhasenId { get; set; }
            public string PhaseKurz { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
            public int VerantwortlicherbenutzerId { get; set; }
            public string? Notizen { get; set; }
            public string? Status { get; set; }

            // Neu: Kennzeichnet, ob der aktuelle Benutzer diese Phase im UI bearbeiten darf
            public bool CanEdit { get; set; }
        }
    }
}