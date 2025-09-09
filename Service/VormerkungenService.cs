// Datei: Service/VormerkungService.cs
// Seite: VormerkungService

using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Service
{
    // Kleine Vormerk-Entity (NEU) – Aufgabe bleibt unverändert
    public class AbhakVormerkung
    {
        [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; }
        public int AufgabeId { get; set; }
        public int AusloeserId { get; set; }
        public DateTime GeplantUtc { get; set; }
        public VormerkAktion Aktion { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public class VormerkungService : IVormerkungService
    {
        private readonly AppDbContext _db;
        public VormerkungService(AppDbContext db) => _db = db;

        public async Task<int> VormerkenAsync(int aufgabeId, int ausloeserId, VormerkAktion aktion, TimeSpan? delay = null, CancellationToken ct = default)
        {
            var when = DateTime.UtcNow + (delay ?? TimeSpan.FromMinutes(2));
            var v = new AbhakVormerkung { AufgabeId = aufgabeId, AusloeserId = ausloeserId, GeplantUtc = when, Aktion = aktion };
            _db.Set<AbhakVormerkung>().Add(v);
            await _db.SaveChangesAsync(ct);
            return v.Id;
        }

        public async Task<bool> UndoAsync(int vormerkId, int currentUserId, CancellationToken ct = default)
        {
            var v = await _db.Set<AbhakVormerkung>().FirstOrDefaultAsync(x => x.Id == vormerkId, ct);
            if (v is null) return false;
            if (v.AusloeserId != currentUserId) return false;                                                         // nur Auslöser darf zurücknehmen
            _db.Remove(v);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> ProcessDueAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var due = await _db.Set<AbhakVormerkung>().Where(v => v.GeplantUtc <= now).OrderBy(v => v.Id).ToListAsync(ct);
            var count = 0;

            foreach (var v in due)
            {
                var a = await _db.Aufgaben.FirstOrDefaultAsync(x => x.Id == v.AufgabeId, ct);
                if (a is null) { _db.Remove(v); continue; }

                if (v.Aktion == VormerkAktion.Loeschen)
                {
                    _db.Aufgaben.Remove(a);                                                                           // physisch löschen (nur bei Selbsterstellt)
                }
                else if (v.Aktion == VormerkAktion.MarkErledigt)
                {
                    a.Erledigt = Erledigungsstatus.Erledigt;                                                          // markiere als erledigt
                }

                _db.Remove(v);
                count++;
            }

            await _db.SaveChangesAsync(ct);
            return count;
        }
    }
}
