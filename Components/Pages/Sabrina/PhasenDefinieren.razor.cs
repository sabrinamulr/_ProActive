using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Components.Pages.Sabrina
{
    public partial class PhasenDefinieren : ComponentBase
    {
        [Parameter] public int ProjectId { get; set; }

        [Inject] private AppDbContext Db { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        protected Projekt? project;
        protected List<Phase> phases = new();
        protected List<Benutzer> allUsers = new();
        protected List<PhaseConfig> selections = new();

        protected bool isLoading = true;
        protected bool isSaving = false;
        protected string? uiError;

        protected override async Task OnParametersSetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            isLoading = true;
            uiError = null;

            try
            {
                project = await Db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == ProjectId);
                if (project == null) return;

                // feste Reihenfolge: Phasen in der DB in definierter Reihenfolge laden (Id aufsteigend)
                phases = await Db.Phasen.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
                allUsers = await Db.Benutzer.AsNoTracking().OrderBy(b => b.Email).ToListAsync();

                var existing = await Db.ProjektPhasen
                    .Where(pp => pp.ProjekteId == ProjectId)
                    .AsNoTracking()
                    .ToListAsync();

                // Build configs: für jede globale Phase immer ein Eintrag (Reihenfolge unveränderlich)
                selections = phases.Select(ph =>
                {
                    var ex = existing.FirstOrDefault(e => e.PhasenId == ph.Id);
                    return new PhaseConfig
                    {
                        Phase = ph,
                        ExistingId = ex?.Id ?? 0,
                        StartDate = ex?.StartDate ?? DateTime.Today,
                        DueDate = ex?.DueDate ?? DateTime.Today.AddDays(14),
                        VerantwortlicherbenutzerId = ex?.VerantwortlicherbenutzerId ?? project.ProjektleiterId,
                        Notizen = ex?.Notizen,
                        Status = ex?.Status ?? "Geplant"
                    };
                }).ToList();
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

        protected async Task SaveAsync()
        {
            if (project == null) return;
            isSaving = true;
            uiError = null;

            try
            {
                // Validierung: Start <= Due für alle
                foreach (var cfg in selections)
                {
                    if (cfg.StartDate.Date > cfg.DueDate.Date)
                    {
                        uiError = $"Fehler: Für Phase '{cfg.Phase.Kurzbezeichnung}' ist Startdatum nach dem Due‑Datum.";
                        isSaving = false;
                        return;
                    }
                    if (!string.IsNullOrEmpty(cfg.Notizen) && cfg.Notizen.Length > PhaseConfig.MaxNotesLength)
                    {
                        uiError = $"Fehler: Notizen für Phase '{cfg.Phase.Kurzbezeichnung}' überschreiten {PhaseConfig.MaxNotesLength} Zeichen.";
                        isSaving = false;
                        return;
                    }
                }

                var existing = await Db.ProjektPhasen.Where(pp => pp.ProjekteId == ProjectId).ToListAsync();

                // Upsert: für jede Phase entweder update oder insert (keine Lösch-Logik, Phasen bleiben in DB)
                foreach (var sel in selections)
                {
                    if (sel.ExistingId != 0)
                    {
                        var upd = existing.FirstOrDefault(e => e.Id == sel.ExistingId);
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
                        var neu = new ProjektPhase
                        {
                            ProjekteId = ProjectId,
                            PhasenId = sel.Phase.Id,
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

                Nav.NavigateTo($"/projekt/{ProjectId}");
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

        protected void Cancel()
        {
            Nav.NavigateTo($"/projekt/{ProjectId}");
        }

        protected class PhaseConfig
        {
            public const int MaxNotesLength = 2000;

            public Phase Phase { get; set; } = default!;
            public int ExistingId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
            public int VerantwortlicherbenutzerId { get; set; }
            public string? Notizen { get; set; }
            public string? Status { get; set; }    // z.B. "Grün"/"Gelb"/"Rot"/"Geplant"
        }
    }
}