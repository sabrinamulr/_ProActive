// Datei: Service/AufgabenService.cs
// Seite: AufgabenService

using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Service
{
    public class AufgabenService : IAufgabenService
    {
        private readonly AppDbContext _db;
        public AufgabenService(AppDbContext db) => _db = db;

        public async Task<List<Aufgabe>> GetOffeneFuerBenutzerAsync(int benutzerId, bool includeDone = false, CancellationToken ct = default)
        {
            var q = _db.Aufgaben.AsNoTracking().Where(a => a.BenutzerId == benutzerId);
            if (!includeDone) q = q.Where(a => a.Erledigt != Erledigungsstatus.Erledigt);
            return await q.OrderBy(a => a.Faellig).ThenBy(a => a.Id).ToListAsync(ct);
        }

        public async Task<List<Aufgabe>> GetZuweisungenVonLeiterAsync(int projektleiterId, CancellationToken ct = default)
        {
            // Aufgaben, die der Leiter an andere vergeben hat (ErstellVon = ich, Bearbeiter != ich) und in Projekten, die ich leite
            var q = from a in _db.Aufgaben.AsNoTracking()
                    join p in _db.Projekte.AsNoTracking() on a.ProjektId equals p.Id
                    where a.ErstellVon == projektleiterId && a.BenutzerId != projektleiterId && p.ProjektleiterId == projektleiterId
                    orderby a.Faellig, a.Id
                    select a;
            return await q.ToListAsync(ct);
        }

        public Task<Aufgabe?> GetByIdAsync(int id, CancellationToken ct = default)
            => _db.Aufgaben.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<Aufgabe> CreateAsync(Aufgabe neu, int currentUserId, bool isProjektleiter, CancellationToken ct = default)
        {
            neu.Id = 0;                                                                                     // DB vergibt Id
            neu.ErstellVon = currentUserId;                                                                 // Ersteller = ich

            if (!isProjektleiter)
            {
                neu.BenutzerId = currentUserId;                                                             // nur mir selbst zuweisen

                // --- WICHTIG: gültige ProjektId erzwingen (kein 0, sonst FK-Fehler) ---
                if (neu.ProjektId == 0)
                {
                    var projId = await _db.Projekte.AsNoTracking()
                        .Where(p => p.BenutzerId == currentUserId || p.ProjektleiterId == currentUserId || p.AuftraggeberId == currentUserId)
                        .Select(p => p.Id)
                        .OrderBy(id => id)
                        .FirstOrDefaultAsync(ct);

                    if (projId == 0) throw new InvalidOperationException("Du bist keinem Projekt zugeordnet. Bitte wende dich an deinen Projektleiter.");
                    neu.ProjektId = projId;
                }
            }
            else
            {
                if (neu.ProjektId == 0) throw new InvalidOperationException("Bitte ein Projekt wählen.");
                var projekt = await _db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == neu.ProjektId, ct);
                if (projekt is null || projekt.ProjektleiterId != currentUserId) throw new UnauthorizedAccessException("Dieses Projekt leitest du nicht.");
                if (neu.BenutzerId == 0) neu.BenutzerId = currentUserId;                                    // Fallback „Ich“
            }

            if (string.IsNullOrWhiteSpace(neu.Aufgabenbeschreibung)) throw new ArgumentException("Beschreibung ist erforderlich.", nameof(neu));

            _db.Aufgaben.Add(neu);
            await _db.SaveChangesAsync(ct);
            return neu;
        }
        public Task<List<Aufgabe>> GetByProjektIdAsync(int projektId)
    => _db.Aufgaben.Where(a => a.ProjektId == projektId).OrderBy(a => a.Faellig).ToListAsync();

        public async Task<bool> UpdateAsync(Aufgabe changed, int currentUserId, bool isProjektleiter, CancellationToken ct = default)
        {
            var tracked = await _db.Aufgaben.FirstOrDefaultAsync(a => a.Id == changed.Id, ct);
            if (tracked is null) return false;

            // Berechtigung: Ersteller ODER (Projektleiter des Projekts)
            var isOwner = tracked.ErstellVon == currentUserId;
            var isLeaderOfProject = isProjektleiter && await _db.Projekte.AsNoTracking().AnyAsync(p => p.Id == tracked.ProjektId && p.ProjektleiterId == currentUserId, ct);
            if (!isOwner && !isLeaderOfProject) return false;

            // Nicht-Projektleiter dürfen nicht um-zuweisen
            if (!isProjektleiter) changed.BenutzerId = tracked.BenutzerId;

            // Projektleiter dürfen Projekt/Bearbeiter ändern – aber nur in eigenen Projekten
            if (isProjektleiter && changed.ProjektId != tracked.ProjektId)
            {
                var proj = await _db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == changed.ProjektId, ct);
                if (proj is null || proj.ProjektleiterId != currentUserId) throw new UnauthorizedAccessException("Dieses Projekt leitest du nicht.");
            }

            // Felder übernehmen
            tracked.ProjektId = changed.ProjektId;
            tracked.BenutzerId = changed.BenutzerId;
            tracked.Aufgabenbeschreibung = changed.Aufgabenbeschreibung;
            tracked.Faellig = changed.Faellig;
            tracked.Phase = changed.Phase;
            tracked.Erledigt = changed.Erledigt;

            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
