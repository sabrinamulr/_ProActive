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

                List<ProjektPhase> existingProjektPhasen = await Db.ProjektPhasen
                    .Where(pp => pp.ProjekteId == ProjectId)
                    .AsNoTracking()
                    .ToListAsync();

                // lade alle Meilenstein‑Vorlagen (global)
                List<Meilenstein> meilensteinVorlagen = await Db.Meilensteine
                    .AsNoTracking()
                    .ToListAsync();

                // Build selections: für jede globale Phase immer ein Eintrag (Reihenfolge unveränderlich)
                selections = new List<PhaseConfig>();
                foreach (var ph in phases)
                {
                    ProjektPhase? ex = existingProjektPhasen.FirstOrDefault(e => e.PhasenId == ph.Id);
                    PhaseMeilenstein? exPm = null;
                    if (ex != null)
                    {
                        exPm = await Db.PhaseMeilensteine
                            .AsNoTracking()
                            .Include(pm => pm.Meilenstein)
                            .FirstOrDefaultAsync(pm => pm.ProjektphasenId == ex.Id);
                    }

                    PhaseConfig cfg = new PhaseConfig
                    {
                        Phase = ph,
                        ExistingId = ex?.Id ?? 0,
                        StartDate = ex?.StartDate ?? DateTime.Today,
                        DueDate = ex?.DueDate ?? DateTime.Today.AddDays(14),
                        VerantwortlicherBenutzerId = ex?.VerantwortlicherbenutzerId ?? project.ProjektleiterId,
                        Notizen = ex?.Notizen,
                        Status = ex?.Status ?? "Geplant",

                        // Meilenstein-Felder: falls bereits projektbezogen vorhanden, sonst Standardwerte
                        MeilensteinZieldatum = exPm?.Zieldatum ?? (ex?.DueDate ?? DateTime.Today.AddDays(14)),
                        MeilensteinErreichtDatum = exPm?.Erreichtdatum,
                        MeilensteinGenehmigerId = exPm?.GenehmigerbenutzerId ?? (ex?.VerantwortlicherbenutzerId ?? project.ProjektleiterId),
                        MeilensteinStatus = exPm?.Status ?? "nicht erreicht"
                    };

                    selections.Add(cfg);
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

        protected async Task SaveAsync()
        {
            if (project == null) return;
            isSaving = true;
            uiError = null;

            try
            {
                // Validierung: Start <= Due für alle und Notizen‑Länge
                foreach (PhaseConfig cfg in selections)
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

                // 1) vorhandene ProjektPhasen laden (zur Upsert-Entscheidung)
                List<ProjektPhase> existingProjektPhasen = await Db.ProjektPhasen.Where(pp => pp.ProjekteId == ProjectId).ToListAsync();

                // 2) Upsert ProjektPhasen
                foreach (PhaseConfig sel in selections)
                {
                    if (sel.ExistingId != 0)
                    {
                        ProjektPhase? upd = existingProjektPhasen.FirstOrDefault(e => e.Id == sel.ExistingId);
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
                            ProjekteId = ProjectId,
                            PhasenId = sel.Phase.Id,
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

                // 3) Nach Save: projektPhasen neu laden und projektbezogene Meilensteine
                List<ProjektPhase> projektPhasenNachSave = await Db.ProjektPhasen
                    .Where(pp => pp.ProjekteId == ProjectId)
                    .ToListAsync();

                List<int> projektPhasenIdsNachSave = projektPhasenNachSave.Select(pp => pp.Id).ToList();
                List<PhaseMeilenstein> existingProjektMeilensteine = new();
                if (projektPhasenIdsNachSave.Count > 0)
                {
                    existingProjektMeilensteine = await Db.PhaseMeilensteine
                        .Where(pm => projektPhasenIdsNachSave.Contains(pm.ProjektphasenId))
                        .Include(pm => pm.Meilenstein)
                        .ToListAsync();
                }

                // 4) lade alle Meilenstein‑Vorlagen (global) — Vorlagen enthalten nur Bezeichnung
                List<Meilenstein> meilensteinVorlagen = await Db.Meilensteine
                    .AsNoTracking()
                    .ToListAsync();

                // 5) Upsert PhaseMeilenstein (weist Vorlagen/Meilensteine der ProjektPhase zu)
                List<PhaseMeilenstein> existingPhaseMeilensteine = await Db.PhaseMeilensteine
                    .Where(pm => projektPhasenIdsNachSave.Contains(pm.ProjektphasenId))
                    .ToListAsync();

                foreach (PhaseConfig sel in selections)
                {
                    ProjektPhase? linkedProjektPhase = projektPhasenNachSave.FirstOrDefault(pp => pp.PhasenId == sel.Phase.Id);
                    if (linkedProjektPhase == null) continue;

                    // Template lookup by exact Bezeichnung == Phase.Kurzbezeichnung (fallback: contains)
                    Meilenstein? template = meilensteinVorlagen.FirstOrDefault(t => string.Equals(t.Bezeichnung?.Trim(), sel.Phase.Kurzbezeichnung?.Trim(), StringComparison.OrdinalIgnoreCase))
                        ?? meilensteinVorlagen.FirstOrDefault(t => (t.Bezeichnung ?? string.Empty).IndexOf(sel.Phase.Kurzbezeichnung ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0);

                    // Verwende nur vorhandene Template-IDs (keine Neuanlage). Fallback auf Phase.Id (Seed-Mapping erwartet Id 1-9).
                    int templateId = template?.Id ?? sel.Phase.Id;

                    // Prüfe, ob die gewählte TemplateId tatsächlich in den vorhandenen Vorlagen existiert.
                    if (!meilensteinVorlagen.Any(m => m.Id == templateId))
                    {
                        uiError = $"Kein gültiges Meilenstein‑Template für Phase '{sel.Phase.Kurzbezeichnung}' vorhanden. Erlaubte Template‑IDs: {string.Join(", ", meilensteinVorlagen.Select(m => m.Id))}.";
                        isSaving = false;
                        return;
                    }

                    PhaseMeilenstein? existingPm = existingPhaseMeilensteine.FirstOrDefault(pm => pm.ProjektphasenId == linkedProjektPhase.Id);

                    if (existingPm != null)
                    {
                        // Update bestehender PhaseMeilenstein
                        existingPm.MeilensteinId = templateId;
                        existingPm.GenehmigerbenutzerId = sel.MeilensteinGenehmigerId;
                        existingPm.Status = sel.MeilensteinStatus;
                        existingPm.Zieldatum = sel.MeilensteinZieldatum;
                        existingPm.Erreichtdatum = sel.MeilensteinErreichtDatum;
                        Db.PhaseMeilensteine.Update(existingPm);
                    }
                    else
                    {
                        // Neues PhaseMeilenstein anlegen (wird nur gültige MeilensteinId verwenden)
                        PhaseMeilenstein neuPm = new PhaseMeilenstein
                        {
                            ProjektphasenId = linkedProjektPhase.Id,
                            MeilensteinId = templateId,
                            GenehmigerbenutzerId = sel.MeilensteinGenehmigerId,
                            Status = sel.MeilensteinStatus,
                            Zieldatum = sel.MeilensteinZieldatum,
                            Erreichtdatum = sel.MeilensteinErreichtDatum
                        };
                        Db.PhaseMeilensteine.Add(neuPm);
                    }

                    // Optional: lege zusätzlich ein projekt‑gebundenes PhaseMeilenstein Entity an/aktualisiere es,
                    // falls dein Modell Meilenstein sowohl als Vorlage als auch als Instanz verwendet.
                    PhaseMeilenstein? msInst = existingProjektMeilensteine.FirstOrDefault(m => m.ProjektphasenId == linkedProjektPhase.Id);
                    if (msInst != null)
                    {
                        msInst.Status = sel.MeilensteinStatus;
                        msInst.Zieldatum = sel.MeilensteinZieldatum;
                        msInst.Erreichtdatum = sel.MeilensteinErreichtDatum;
                        msInst.GenehmigerbenutzerId = sel.MeilensteinGenehmigerId;
                        Db.PhaseMeilensteine.Update(msInst);
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

        public class PhaseConfig
        {
            public const int MaxNotesLength = 2000;

            public Phase Phase { get; set; } = default!;
            public int ExistingId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime DueDate { get; set; }
            public int VerantwortlicherBenutzerId { get; set; }
            public string? Notizen { get; set; }
            public string? Status { get; set; }    // z. B. "Grün"/"Gelb"/"Rot"/"Geplant"

            // Meilenstein-spezifische Felder (vom Projektleiter gesetzt)
            public DateTime MeilensteinZieldatum { get; set; }
            public DateTime? MeilensteinErreichtDatum { get; set; }
            public int MeilensteinGenehmigerId { get; set; }
            public string? MeilensteinStatus { get; set; } // "nicht erreicht","erreicht","freigegeben"
        }
    }
}