//sicherheitspush

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;
using System.Linq;
using System.Collections.Generic;

namespace ProActive2508.Components.Pages.Sabrina
{
    public partial class ProjekteNeu : ComponentBase
    {
        protected NewProjectModel model = new();
        protected bool isSaving = false;
        protected string? uiError;

        [Inject] private AppDbContext Db { get; set; } = default!;
        [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private int CurrentUserId;

        // Mitglieder
        private List<Benutzer> allUsers = new();
        private List<int> selectedMemberIds = new();

        // Phasen (wie beim Bearbeiten)
        private List<PhaseEditConfig> editPhaseSelections = new();

        protected override async Task OnInitializedAsync()
        {
            AuthenticationState auth = await Auth.GetAuthenticationStateAsync();
            System.Security.Claims.ClaimsPrincipal user = auth.User;

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

            // lade alle Benutzer als Auswahl
            allUsers = await Db.Benutzer.AsNoTracking().OrderBy(b => b.Email).ToListAsync();

            // Default-Phasen wie beim Edit: mindestens eine Phase vorbefüllen
            var first = await Db.Phasen.AsNoTracking().OrderBy(p => p.Id).FirstOrDefaultAsync();
            if (first != null)
            {
                editPhaseSelections = new List<PhaseEditConfig>
                {
                    new PhaseEditConfig
                    {
                        ExistingId = 0,
                        PhaseId = first.Id,
                        PhaseKurz = first.Kurzbezeichnung,
                        StartDate = DateTime.Today,
                        DueDate = DateTime.Today.AddDays(14),
                        VerantwortlicherBenutzerId = CurrentUserId,
                        Status = "Geplant",
                        CanEdit = true
                    }
                };
            }
        }

        // Standard-Save (Form-Submit)
        protected async Task SaveAsync()
        {
            await SaveCoreAsync(redirectToPhases: false);
        }

        // Aufruf durch "Weiter"
        protected async Task SaveAndDefineAsync()
        {
            await SaveCoreAsync(redirectToPhases: true);
        }

        // Core-Create: gibt bei Erfolg neue Projekt-Id zurück und navigiert je nach Flag
        private async Task<int> SaveCoreAsync(bool redirectToPhases)
        {
            uiError = null;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                uiError = "Name ist erforderlich.";
                return 0;
            }

            // validate phases
            foreach (var cfg in editPhaseSelections)
            {
                if (cfg.StartDate.Date > cfg.DueDate.Date)
                {
                    uiError = $"Fehler: Für Phase '{cfg.PhaseKurz}' ist Startdatum nach dem Due?Datum.";
                    return 0;
                }
                if (!string.IsNullOrEmpty(cfg.Notizen) && cfg.Notizen.Length > PhasenDefinieren.PhaseConfig.MaxNotesLength)
                {
                    uiError = $"Fehler: Notizen für Phase '{cfg.PhaseKurz}' überschreiten {PhasenDefinieren.PhaseConfig.MaxNotesLength} Zeichen.";
                    return 0;
                }
            }

            isSaving = true;

            try
            {
                // Lese die erste Phase aus der DB (feste, vordefinierte Reihenfolge)
                Phase? firstPhase = await Db.Phasen.AsNoTracking().OrderBy(p => p.Id).FirstOrDefaultAsync();
                int phaseId = firstPhase?.Id ?? 0;

                Projekt projekt = new Projekt
                {
                    BenutzerId = CurrentUserId,
                    ProjektleiterId = CurrentUserId,
                    AuftraggeberId = CurrentUserId,
                    Projektbeschreibung = model.Name + (string.IsNullOrWhiteSpace(model.Description) ? string.Empty : " – " + model.Description),
                    Status = Projektstatus.Aktiv,
                    Phase = phaseId
                };

                // Use transaction to create project, phases and members atomically
                await using var tx = await Db.Database.BeginTransactionAsync();

                Db.Projekte.Add(projekt);
                await Db.SaveChangesAsync();

                int newId = projekt.Id;

                // create ProjektPhasen from editPhaseSelections
                if (editPhaseSelections != null && editPhaseSelections.Any())
                {
                    foreach (var sel in editPhaseSelections)
                    {
                        var neu = new ProjektPhase
                        {
                            ProjekteId = newId,
                            PhasenId = sel.PhaseId,
                            StartDate = sel.StartDate,
                            DueDate = sel.DueDate,
                            VerantwortlicherbenutzerId = sel.VerantwortlicherBenutzerId,
                            Status = sel.Status,
                            Notizen = sel.Notizen
                        };
                        Db.ProjektPhasen.Add(neu);
                    }
                    await Db.SaveChangesAsync();
                }

                // Sync members
                if (selectedMemberIds != null && selectedMemberIds.Any())
                {
                    foreach (var uid in selectedMemberIds)
                    {
                        Db.ProjektBenutzer.Add(new ProjektBenutzer { ProjektId = newId, BenutzerId = uid });
                    }
                    await Db.SaveChangesAsync();
                }

                await tx.CommitAsync();

                if (redirectToPhases)
                {
                    Nav.NavigateTo($"/projekt/{newId}/phasen-definieren");
                }
                else
                {
                    Nav.NavigateTo("/meine-projekte");
                }

                return newId;
            }
            catch (Exception ex)
            {
                uiError = ex.InnerException?.Message ?? ex.Message;
                return 0;
            }
            finally
            {
                isSaving = false;
            }
        }

        protected void Cancel()
        {
            Nav.NavigateTo("/");
        }

        protected class NewProjectModel
        {
            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; } = string.Empty;

            public string? Description { get; set; }

            public DateTime? ExpectedCompletionDate { get; set; }
        }

        private void RemoveMember(int uid)
        {
            if (selectedMemberIds.Contains(uid)) selectedMemberIds.Remove(uid);
        }

        private void OnMembersChanged(ChangeEventArgs e)
        {
            try
            {
                selectedMemberIds.Clear();
                if (e?.Value is not null)
                {
                    var raw = e.Value.ToString() ?? string.Empty;
                    var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var p in parts)
                    {
                        if (int.TryParse(p, out int id)) selectedMemberIds.Add(id);
                    }
                }
            }
            catch
            {
                // ignore parsing errors
            }
        }

        private void ToggleMember(int id, bool isChecked)
        {
            if (isChecked)
            {
                if (!selectedMemberIds.Contains(id)) selectedMemberIds.Add(id);
            }
            else
            {
                selectedMemberIds.Remove(id);
            }
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
            public bool CanEdit { get; set; }
        }
    }
}