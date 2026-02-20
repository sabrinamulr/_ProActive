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
    // Detailansicht für ein einzelnes Projekt.
    // Verantwortlich für:
    // - Laden des Projekts und zugehöriger Phasen, Mitglieder und Aufgaben
    // - Bestimmung der aktuellen Phase
    // - Steuern eines Edit-Modals für das Projekt
    public partial class ProjektDetails : ComponentBase
    {
        [Parameter] public int Id { get; set; }

        protected Projekt? project;
        protected Projekt? editModel;
        protected bool isLoading = true;
        protected bool isEditing = false;
        protected Dictionary<int, string> _userLookup = new();

        // Phasen-Daten & Rollenflag
        protected List<ProjektPhase>? projectPhases;
        protected ProjektPhase? currentPhase;
        protected bool isProjektleiterRole = false;

        // aktuelle Benutzer-Id
        private int CurrentUserId;

        // Aufgaben für dieses Projekt
        protected List<Aufgabe> projektAufgaben = new List<Aufgabe>();
        protected Dictionary<int, string> aufgabenBenutzerLookup = new();

        // Mitglieder
        protected List<Benutzer> projectMembers = new List<Benutzer>();

        // Modal state
        protected int editingProjectId = 0;

        [Inject] private AppDbContext Db { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        // OnParametersSetAsync: ruft LoadAsync auf, wenn Parameter (z. B. Id) sich ändern
        protected override async Task OnParametersSetAsync()
        {
            await LoadAsync();
        }

        // LoadAsync: Lädt alle relevanten Daten für die Detailansicht
        // - ermittelt aktuellen Benutzer
        // - lädt Projekt, Benutzer-Lookup, Phasen, Aufgaben und Mitglieder
        private async Task LoadAsync()
        {
            isLoading = true;
            try
            {
                AuthenticationState auth = await AuthenticationStateTask;
                System.Security.Claims.ClaimsPrincipal user = auth.User;

                string? idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;
                int currentUserId = 0;
                if (!int.TryParse(idClaim, out currentUserId) || currentUserId <= 0)
                {
                    string? name = user.Identity?.Name ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name) && int.TryParse(name, out int pn))
                    {
                        Benutzer? dbUser = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == pn);
                        currentUserId = dbUser?.Id ?? 0;
                    }
                    else
                    {
                        Benutzer? dbUserByMail = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Email == name);
                        currentUserId = dbUserByMail?.Id ?? 0;
                    }
                }

                // speichere die aktuelle BenutzerId
                CurrentUserId = currentUserId;

                project = await Db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == Id);
                if (project is null)
                {
                    // initialisiere leere Lookups
                    _userLookup = new Dictionary<int, string>();
                    projectPhases = new List<ProjektPhase>();
                    currentPhase = null;
                    isProjektleiterRole = false;

                    projektAufgaben = new List<Aufgabe>();
                    aufgabenBenutzerLookup = new Dictionary<int, string>();

                    projectMembers = new List<Benutzer>();
                    return;
                }

                List<int> userIds = new List<int> { project.ProjektleiterId, project.AuftraggeberId };
                userIds = userIds.Where(i => i > 0).Distinct().ToList();
                if (userIds.Count > 0)
                {
                    Dictionary<int, string> lookup = await Db.Benutzer
                        .AsNoTracking()
                        .Where(b => userIds.Contains(b.Id))
                        .ToDictionaryAsync(b => b.Id, b => string.IsNullOrWhiteSpace(b.Email) ? $"User#{b.Id}" : b.Email);
                    _userLookup = lookup;
                }
                else
                {
                    _userLookup = new Dictionary<int, string>();
                }

                // Berechtigung prüfen: Rolle Projektleiter
                isProjektleiterRole = user.IsInRole("Projektleiter");

                // Phasen + aktuelle Phase laden
                projectPhases = await Db.ProjektPhasen
                    .AsNoTracking()
                    .Where(pp => pp.ProjekteId == project.Id)
                    .Include(pp => pp.Phase)
                    .OrderBy(pp => pp.StartDate)
                    .ToListAsync();

                if (projectPhases != null && projectPhases.Count > 0)
                {
                    ProjektPhase? active = null;
                    DateTime today = DateTime.Today;

                    // suche aktive Phase nach Datum
                    active = projectPhases.FirstOrDefault(pp =>
                        pp.StartDate.Date <= today && pp.DueDate.Date >= today && (pp.Abschlussdatum == null || pp.Abschlussdatum.Value.Date >= today));

                    if (active == null)
                    {
                        active = projectPhases.Where(pp => pp.StartDate.Date <= today).OrderByDescending(pp => pp.StartDate).FirstOrDefault();
                    }

                    if (active == null)
                    {
                        active = projectPhases.OrderBy(pp => pp.StartDate).FirstOrDefault();
                    }

                    currentPhase = active;
                }
                else
                {
                    currentPhase = null;
                }

                // Aufgaben für dieses Projekt laden
                projektAufgaben = await Db.Set<Aufgabe>()
                    .AsNoTracking()
                    .Where(a => a.ProjektId.HasValue && a.ProjektId.Value == project.Id)
                    .OrderBy(a => a.Faellig)
                    .ToListAsync();

                List<int> benutzerIds = projektAufgaben.Select(a => a.BenutzerId).Where(id => id > 0).Distinct().ToList();
                if (benutzerIds.Count > 0)
                {
                    aufgabenBenutzerLookup = await Db.Benutzer
                        .AsNoTracking()
                        .Where(b => benutzerIds.Contains(b.Id))
                        .ToDictionaryAsync(b => b.Id, b => string.IsNullOrWhiteSpace(b.Email) ? $"User#{b.Id}" : b.Email);
                }
                else
                {
                    aufgabenBenutzerLookup = new Dictionary<int, string>();
                }

                // Mitglieder für dieses Projekt laden
                List<int> memberIds = await Db.ProjektBenutzer
                    .AsNoTracking()
                    .Where(pb => pb.ProjektId == project.Id)
                    .Select(pb => pb.BenutzerId)
                    .Distinct()
                    .ToListAsync();

                if (memberIds != null && memberIds.Count > 0)
                {
                    List<Benutzer> benutzer = await Db.Benutzer
                        .AsNoTracking()
                        .Where(b => memberIds.Contains(b.Id))
                        .OrderBy(b => b.Email)
                        .ToListAsync();

                    projectMembers = benutzer;
                }
                else
                {
                    projectMembers = new List<Benutzer>();
                }
            }
            catch
            {
                // Bei Fehlern werden alle View-Daten wieder auf sichere leere Werte gesetzt
                project = null;
                _userLookup = new Dictionary<int, string>();
                projectPhases = new List<ProjektPhase>();
                currentPhase = null;
                isProjektleiterRole = false;

                projektAufgaben = new List<Aufgabe>();
                aufgabenBenutzerLookup = new Dictionary<int, string>();

                projectMembers = new List<Benutzer>();
            }
            finally
            {
                isLoading = false;
            }
        }

        // Prüft, ob das Projekt bearbeitet werden darf (UI-Logik)
        protected bool CanEdit(Projekt? p)
        {
            if (p is null) return false;
            return isProjektleiterRole || p.ProjektleiterId == CurrentUserId;
        }

        // EnableEdit: bereitet das editModel vor und setzt Editing-Flag
        protected void EnableEdit()
        {
            if (project is null)
            {
                return;
            }
            else 
            {
                editModel = new Projekt
                {
                    Id = project.Id,
                    Projektbeschreibung = project.Projektbeschreibung,
                    BenutzerId = project.BenutzerId,
                    ProjektleiterId = project.ProjektleiterId,
                    AuftraggeberId = project.AuftraggeberId,
                    Status = project.Status,
                    Phase= project.Phase, 
                };
                isEditing = true;
            }
                
        }

        // SaveAsync: speichert Änderungen am Projekt (vereinfachte Variante)
        protected async Task SaveAsync()
        {
            if (editModel is null) return;
            try
            {
                Db.Projekte.Update(editModel);
                await Db.SaveChangesAsync();
                isEditing = false;
                await LoadAsync();
            }
            catch (Exception)
            {
                // Fehlerbehandlung nach Bedarf
            }
        }

        protected void CancelEdit()
        {
            isEditing = false;
            editModel = null;
        }

        // Modal callbacks
        protected async Task OnModalSaved()
        {
            editingProjectId = 0;
            await LoadAsync();
            StateHasChanged();
        }

        protected Task OnModalCancelled()
        {
            editingProjectId = 0;
            return Task.CompletedTask;
        }

        protected void OpenEditModal(int projektId)
        {
            editingProjectId = projektId;
            InvokeAsync(StateHasChanged);
        }
    }
}