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
        // Daten für View (werden im OnInitializedAsync geladen)
        protected List<Projekt>? projects;
        protected Dictionary<int, string> userLookup = new();
        protected bool isLoading = true;
        protected string? uiError;

        // Steuerung Modal
        protected int editingProjectId = 0;

        // Phasen-Lookups
        protected Dictionary<int, List<ProjektPhase>> projectPhasesLookup = new();
        protected Dictionary<int, ProjektPhase?> currentPhaseLookup = new();
        protected bool isProjektleiterRole = false;

        // für Modal-Edit: Phase-Selections + Benutzerliste (falls benötigt)
        protected List<PhaseEditConfig> editPhaseSelections = new();
        protected List<Benutzer> allUsers = new();

        [Inject] private AppDbContext Db { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;
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
                    projects = await Db.Projekte
                        .AsNoTracking()
                        //.Include(p => p.PhaseDefinition)
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
                        //.Include(p => p.PhaseDefinition)
                        .OrderBy(p => p.Id)
                        .ToListAsync();
                }

                // userLookup
                if (projects != null && projects.Any())
                {
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
                }
                else
                {
                    userLookup = new Dictionary<int, string>();
                }

                // --- Lade Phasen für angezeigte Projekte ---
                List<int> projectIds = projects?.Select(p => p.Id).ToList() ?? new List<int>();
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

        // Öffnet Modal (setzt Projekt-Id)
        protected async Task OpenEditModal(int projektId)
        {
            editingProjectId = projektId;
            await InvokeAsync(StateHasChanged);
        }

        // Callback: Modal hat gespeichert → schließen + neu laden
        protected async Task ModalSaved()
        {
            editingProjectId = 0;
            // Seite neu laden, damit Liste & Lookups aktualisiert werden
            Nav.NavigateTo(Nav.Uri, forceLoad: false);
            await Task.CompletedTask;
        }

        // Callback: Modal abgebrochen → nur schließen
        protected Task ModalCancelled()
        {
            editingProjectId = 0;
            return Task.CompletedTask;
        }

        protected class PhaseEditConfig
        {
            public int ExistingId { get; set; }
            public int PhaseId { get; set; }
            public string PhaseKurz { get; set; } = string.Empty;
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
            public int VerantwortlicherBenutzerId { get; set; }
            public string? Notizen { get; set; }
            public string? Status { get; set; }

            // Neu: Kennzeichnet, ob der aktuelle Benutzer diese Phase im UI bearbeiten darf
            public bool CanEdit { get; set; }
        }
    }
}