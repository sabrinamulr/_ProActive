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
    public partial class ProjektDetails : ComponentBase
    {
        [Parameter] public int Id { get; set; }

        protected Projekt? project;
        protected Projekt? editModel;
        protected bool isLoading = true;
        protected bool isEditing = false;
        protected Dictionary<int, string> _userLookup = new();

        // neu: Phasen-Daten & Rollenflag
        protected List<ProjektPhase>? projectPhases;
        protected ProjektPhase? currentPhase;
        protected bool isProjektleiterRole = false;

        // neu: aktuelle Benutzer-Id speichern
        private int CurrentUserId;

        [Inject] private AppDbContext Db { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            await LoadAsync();
        }

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

                // speichere die aktuelle BenutzerId für spätere Berechnungen
                CurrentUserId = currentUserId;

                project = await Db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == Id);
                if (project is null)
                {
                    // keep lookups empty
                    _userLookup = new Dictionary<int, string>();
                    projectPhases = new List<ProjektPhase>();
                    currentPhase = null;
                    isProjektleiterRole = false;
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

                // Berechtigung prüfen: Rolle Projektleiter oder spezifischer Projektleiter
                isProjektleiterRole = user.IsInRole("Projektleiter");
                // note: CanEdit wird über Methode berechnet (siehe unten)

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

                    // bevorzugt: aktive Phase (StartDate <= today <= DueDate and Abschlussdatum null or >= today)
                    active = projectPhases.FirstOrDefault(pp =>
                        pp.StartDate.Date <= today && pp.DueDate.Date >= today && (pp.Abschlussdatum == null || pp.Abschlussdatum.Value.Date >= today));

                    // fallback: die letzte gestartete Phase vor heute
                    if (active == null)
                    {
                        active = projectPhases.Where(pp => pp.StartDate.Date <= today).OrderByDescending(pp => pp.StartDate).FirstOrDefault();
                    }

                    // sonst: erste Phase
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
            }
            catch
            {
                project = null;
                _userLookup = new Dictionary<int, string>();
                projectPhases = new List<ProjektPhase>();
                currentPhase = null;
                isProjektleiterRole = false;
            }
            finally
            {
                isLoading = false;
            }
        }

        // Methode zur Berechnung der Bearbeitungsberechtigung (wird in Razor aufgerufen)
        protected bool CanEdit(Projekt? p)
        {
            if (p is null) return false;
            return isProjektleiterRole || p.ProjektleiterId == CurrentUserId;
        }

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
                    //PhaseDefinition = project.PhaseDefinition, // Optional, falls benötigt
                };
                isEditing = true;
            }
                
        }

        protected async Task SaveAsync()
        {
            if (editModel is null) return;
            // Serverseitige Berechtigungsprüfung nicht nochmals gezeigt, aber empfohlen
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
    }
}