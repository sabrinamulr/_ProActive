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

        protected override async Task OnParametersSetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            isLoading = true;
            uiError = null;
            editPhaseSelections.Clear();
            allUsers = new();

            try
            {
                if (ProjectId <= 0) return;

                // Auth / current user
                var auth = await Auth.GetAuthenticationStateAsync();
                var user = auth.User;
                isProjektleiterRole = user.IsInRole("Projektleiter");
                string? idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                  ?? user.FindFirst("sub")?.Value;
                int parsed = 0;
                if (!int.TryParse(idClaim, out parsed) || parsed <= 0)
                {
                    string? name = user.Identity?.Name ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name) && int.TryParse(name, out int pn))
                    {
                        var dbUser = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == pn);
                        parsed = dbUser?.Id ?? 0;
                    }
                    else
                    {
                        var dbUserByMail = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Email == name);
                        parsed = dbUserByMail?.Id ?? 0;
                    }
                }
                CurrentUserId = parsed;

                // load project
                editModel = await Db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == ProjectId);
                if (editModel == null) { uiError = "Projekt nicht gefunden."; return; }

                // users
                allUsers = await Db.Benutzer.AsNoTracking().OrderBy(b => b.Email).ToListAsync();

                // phases (existing) — use correct property names: ProjektId / PhaseId
                var existing = await Db.ProjektPhasen
                    .AsNoTracking()
                    .Where(pp => pp.ProjekteId == ProjectId)
                    .Include(pp => pp.Phase)
                    .OrderBy(pp => pp.StartDate)
                    .ToListAsync();

                if (!existing.Any())
                {
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
                                VerantwortlicherBenutzerId = editModel.ProjektleiterId,
                                Status = "Geplant",
                                CanEdit = isProjektleiterRole || editModel.ProjektleiterId == CurrentUserId
                            }
                        };
                    }
                }
                else
                {
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
            foreach (var cfg in editPhaseSelections)
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
                // Update Projekt safely: avoid duplicate tracked instances
                var dbProj = await Db.Projekte.FindAsync(editModel.Id);
                if (dbProj == null)
                {
                    // Projekt not tracked / not present -> add as new (should not normally happen for edit)
                    Db.Projekte.Add(editModel);
                }
                else
                {
                    // Apply scalar property changes to the tracked entity
                    Db.Entry(dbProj).CurrentValues.SetValues(editModel);
                }

                // Upsert ProjektPhasen (use ProjekteId / PhaseId)
                List<ProjektPhase> existing = await Db.ProjektPhasen.Where(pp => pp.ProjekteId == editModel.Id).ToListAsync();
                foreach (var sel in editPhaseSelections)
                {
                    if (!isProjektleiterRole)
                    {
                        if (sel.ExistingId != 0)
                        {
                            var dbPhase = existing.FirstOrDefault(e => e.Id == sel.ExistingId);
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
                        var upd = existing.FirstOrDefault(e => e.Id == sel.ExistingId);
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
                        var neu = new ProjektPhase
                        {
                            ProjekteId = editModel.Id,
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

                await Db.SaveChangesAsync();

                // fertig: Callback an Parent
                if (OnSaved.HasDelegate)
                {
                    await OnSaved.InvokeAsync();
                }
                else
                {
                    // Fallback: aktualisiere Seite / navigiere zur aktuellen URI
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