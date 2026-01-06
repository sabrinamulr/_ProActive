using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Service
{
    public class ProjektPhaseService : IProjektPhaseService
    {
        private readonly AppDbContext _db;

        public ProjektPhaseService(AppDbContext db) => _db = db;

        public async Task<bool> TryAdvancePhaseAsync(int projectId, int projektPhaseId, int performedByUserId, CancellationToken ct = default)
        {
            // Load PhaseMeilenstein (must exist) and ensure Status == "freigegeben"
            PhaseMeilenstein? pm = await _db.PhaseMeilensteine
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjektphasenId == projektPhaseId, ct);

            if (pm is null) return false;
            if (!string.Equals(pm.Status, "freigegeben", StringComparison.OrdinalIgnoreCase)) return false;

            // Transactional: mark current ProjektPhase as abgeschlossen and (optionally) activate next
            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                ProjektPhase? current = await _db.ProjektPhasen.FirstOrDefaultAsync(pp => pp.Id == projektPhaseId, ct);
                if (current is null) { await tx.RollbackAsync(ct); return false; }

                // Set Abschlussdatum wenn noch nicht gesetzt
                if (current.Abschlussdatum == null)
                {
                    current.Abschlussdatum = DateTime.UtcNow;
                    _db.ProjektPhasen.Update(current);
                }

                // Bestimme nächste ProjektPhase (nach StartDate)
                ProjektPhase? next = await _db.ProjektPhasen
                    .Where(pp => pp.ProjekteId == projectId && pp.StartDate > current.StartDate)
                    .OrderBy(pp => pp.StartDate)
                    .FirstOrDefaultAsync(ct);

                if (next != null)
                {
                    // Option: setze keinen magischen Enum‑Wert auf Projekt.Phase, da Enum ggf. nicht 1:1 (siehe Hinweis).
                    // Stattdessen können wir Next.StartDate/Status belassen; optional setze einen Indikator auf Projekt:
                    // z.B. Projekt.Phase bleiben unverändert oder erweitern wir später.
                }

                // optional: Audit (nicht implementiert hier) — du kannst hier eine Audit‑Tabelle füllen

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return true;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}