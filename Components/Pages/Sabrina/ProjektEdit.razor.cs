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
    // Modal-Komponente zum Bearbeiten eines Projekts.
    // Zuständigkeiten:
    // - Laden des Projekts und zugehöriger Hilfsdaten (Phasen, Benutzer, Mitglieder)
    // - Validierung und Speichern (Upsert) von Projekt, Phasen und Mitgliedern in einer Transaktion
    public partial class ProjektEdit
    {
        [Parameter] public int ProjectId { get; set; }
        [Parameter] public EventCallback OnSaved { get; set; }
        [Parameter] public EventCallback OnCancelled { get; set; }

        [Inject] public NavigationManager Nav { get; set; } = null!;
        [Inject] public AppDbContext Db { get; set; } = null!;
        [Inject] public AuthenticationStateProvider Auth { get; set; } = null!;

        private Projekt? editModel;
        private List<PhaseEditConfig> editPhaseSelections = new();
        private List<Benutzer> allUsers = new();
        private bool isLoading = true;
        private bool isSaving = false;
        private string? uiError;
        private bool isProjektleiterRole = false;
        private int CurrentUserId;

        // Mitgliederverwaltung (IDs, die aktuell ausgewählt sind)
        private List<int> selectedMemberIds = new();

        // OnParametersSetAsync: Lade Daten, wenn ProjectId gesetzt oder geändert wurde
        protected override async Task OnParametersSetAsync()
        {
            await LoadAsync();
        }

        // LoadAsync: Lädt Projekt, User-Liste, vorhandene Projektphasen und Mitglieder
        // - bestimmt CurrentUserId und Rolle
        // - erstellt editPhaseSelections (falls keine Phasen vorhanden, wird eine Default-Phase gesetzt)
        private async Task LoadAsync()
        {
            isLoading = true;
            uiError = null;
            editPhaseSelections.Clear();
            allUsers = new();
            selectedMemberIds = new();

            try
            {
                if (ProjectId <= 0) return;

                // Auth / current user
                AuthenticationState auth = await Auth.GetAuthenticationStateAsync();
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

                // load project
                Projekt? proj = await Db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == ProjectId);
                editModel = proj;
                if (editModel == null) { uiError = "Projekt nicht gefunden."; return; }

                // users
                List<Benutzer> users = await Db.Benutzer.AsNoTracking().OrderBy(b => b.Email).ToListAsync();
                allUsers = users;

                // load project members
                List<ProjektBenutzer> members = await Db.ProjektBenutzer.AsNoTracking().Where(pb => pb.ProjektId == ProjectId).ToListAsync();
                selectedMemberIds = members.Select(m => m.BenutzerId).ToList();

                // phases (existing)
                List<ProjektPhase> existing = await Db.ProjektPhasen
                    .AsNoTracking()
                    .Where(pp => pp.ProjekteId == ProjectId)
                    .Include(pp => pp.Phase)
                    .OrderBy(pp => pp.StartDate)
                    .ToListAsync();

                if (!existing.Any())
                {
                    // Falls keine Phasen angelegt sind: Default-Phase als Platzhalter
                    Phase? first = await Db.Phasen.AsNoTracking().OrderBy(p => p.Id).FirstOrDefaultAsync();
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
                                VerantwortlicherBenutzerId = editModel.ProjektleiterId,
                                Status = "Geplant",
                                CanEdit = isProjektleiterRole || editModel.ProjektleiterId == CurrentUserId
                            }
                        };
                    }
                }
                else
                {
                    // Vorhandene Phasen in editPhaseSelections transformieren
                    editPhaseSelections = existing.Select(pp => new PhaseEditConfig
                    {
                        ExistingId = pp.Id,
                        PhaseId = pp.PhasenId,
                        PhaseKurz = pp.Phase?.Kurzbezeichnung ?? $"Phase {pp.PhasenId}",
                        StartDate = pp.StartDate,
                        DueDate = pp.DueDate,
                        VerantwortlicherBenutzerId = pp.VerantwortlicherbenutzerId,
                        Status = pp.Status,
                        Notizen = pp.Notizen,
                        CanEdit = isProjektleiterRole || pp.VerantwortlicherbenutzerId == CurrentUserId || editModel.ProjektleiterId == CurrentUserId
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                uiError = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                isLoading = false;
            }
        }

        // ToggleMember: Checkbox-Wechsel für Mitgliederliste
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

        // RemoveMember: entfernt einen Benutzer aus der Mitgliedsliste
        private void RemoveMember(int userId)
        {
            if (selectedMemberIds != null && selectedMemberIds.Contains(userId))
            {
                selectedMemberIds.Remove(userId);
            }
        }

        // SaveAsync: Speichert alle Änderungen (Projekt, Phasen, Mitglieder) atomar in einer DB-Transaktion.
        // Enthält Validierungen und Berechtigungsprüfungen.
        private async Task SaveAsync()
        {
            if (editModel is null) return;

            // server-side permission check
            if (!(isProjektleiterRole || editModel.ProjektleiterId == CurrentUserId))
            {
                uiError = "Keine Berechtigung zum Bearbeiten dieses Projekts.";
                return;
            }

            // validation
            foreach (PhaseEditConfig cfg in editPhaseSelections)
            {
                if (cfg.StartDate.Date > cfg.DueDate.Date)
                {
                    uiError = $"Fehler: Für Phase '{cfg.PhaseKurz}' ist Startdatum nach dem Due‑Datum.";
                    return;
                }
                if (!string.IsNullOrEmpty(cfg.Notizen) && cfg.Notizen.Length > PhasenDefinieren.PhaseConfig.MaxNotesLength)
                {
                    uiError = $"Fehler: Notizen für Phase '{cfg.PhaseKurz}' überschreiten {PhasenDefinieren.PhaseConfig.MaxNotesLength} Zeichen.";
                    return;
                }
            }

            isSaving = true;
            try
            {
                Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await Db.Database.BeginTransactionAsync();
                await using (tx)
                {
                    // Update Projekt sicher: tracked entity vermeiden
                    Projekt? dbProj = await Db.Projekte.FindAsync(ProjectId);
                    if (dbProj == null)
                    {
                        Db.Projekte.Add(editModel);
                        await Db.SaveChangesAsync();
                        dbProj = editModel; // jetzt Id gesetzt
                    }
                    else
                    {
                        Db.Entry(dbProj).CurrentValues.SetValues(editModel);
                        await Db.SaveChangesAsync();
                    }

                    int targetProjektId = dbProj.Id;

                    // Upsert ProjektPhasen
                    List<ProjektPhase> existing = await Db.ProjektPhasen.Where(pp => pp.ProjekteId == targetProjektId).ToListAsync();
                    foreach (PhaseEditConfig sel in editPhaseSelections)
                    {
                        if (!isProjektleiterRole)
                        {
                            if (sel.ExistingId != 0)
                            {
                                ProjektPhase? dbPhase = existing.FirstOrDefault(e => e.Id == sel.ExistingId);
                                if (dbPhase == null || dbPhase.VerantwortlicherbenutzerId != CurrentUserId)
                                {
                                    uiError = $"Keine Berechtigung, Phase '{sel.PhaseKurz}' zu ändern.";
                                    isSaving = false;
                                    return;
                                }
                            }
                            else
                            {
                                if (sel.VerantwortlicherBenutzerId != CurrentUserId && editModel.ProjektleiterId != CurrentUserId)
                                {
                                    uiError = $"Keine Berechtigung, neue Phase '{sel.PhaseKurz}' anzulegen.";
                                    isSaving = false;
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
                                upd.VerantwortlicherbenutzerId = sel.VerantwortlicherBenutzerId;
                                upd.Notizen = sel.Notizen;
                                upd.Status = sel.Status;
                                Db.ProjektPhasen.Update(upd);
                            }
                        }
                        else
                        {
                            ProjektPhase neu = new ProjektPhase
                            {
                                ProjekteId = targetProjektId,
                                PhasenId = sel.PhaseId,
                                StartDate = sel.StartDate,
                                DueDate = sel.DueDate,
                                VerantwortlicherbenutzerId = sel.VerantwortlicherBenutzerId,
                                Status = sel.Status,
                                Notizen = sel.Notizen
                            };
                            Db.ProjektPhasen.Add(neu);
                        }
                    }

                    // Sync ProjektBenutzer (Mitglieder)
                    List<ProjektBenutzer> existingMembers = await Db.ProjektBenutzer.Where(pb => pb.ProjektId == targetProjektId).ToListAsync();
                    List<int> existingIds = existingMembers.Select(m => m.BenutzerId).ToList();

                    List<int> toAdd = (selectedMemberIds ?? new List<int>()).Except(existingIds).ToList();
                    List<int> toRemove = existingIds.Except((selectedMemberIds ?? new List<int>())).ToList();

                    foreach (int uid in toAdd)
                    {
                        Db.ProjektBenutzer.Add(new ProjektBenutzer { ProjektId = targetProjektId, BenutzerId = uid });
                    }

                    if (toRemove.Any())
                    {
                        List<ProjektBenutzer> removes = existingMembers.Where(e => toRemove.Contains(e.BenutzerId)).ToList();
                        Db.ProjektBenutzer.RemoveRange(removes);
                    }



                    await Db.SaveChangesAsync();

                    await tx.CommitAsync();
                }

                // Callback an Parent
                if (OnSaved.HasDelegate)
                {
                    await OnSaved.InvokeAsync();
                }
                else
                {
                    Nav.NavigateTo(Nav.Uri, forceLoad: false);
                }
            }
            catch (Exception ex)
            {
                uiError = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task Cancel()
        {
            if (OnCancelled.HasDelegate) await OnCancelled.InvokeAsync();
        }

        // Hilfsklasse: Phase-Konfiguration im Editor
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