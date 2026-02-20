using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Service
{
    public class ProjekteService : IProjekteService
    {
        private readonly AppDbContext _db;
        public ProjekteService(AppDbContext db) => _db = db;

        public Task<List<Projekt>> GetAllAsync(CancellationToken ct = default)
            => _db.Projekte.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);

        public Task<List<Projekt>> GetProjekteFuerLeiterAsync(int projektleiterId, CancellationToken ct = default)
            => _db.Projekte.AsNoTracking().Where(p => p.ProjektleiterId == projektleiterId).OrderBy(p => p.Id).ToListAsync(ct);

        public async Task<List<Benutzer>> GetMitarbeiterFuerProjektAsync(int projektId, bool includeSelf = true, CancellationToken ct = default)
        {
            var p = await _db.Projekte.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projektId, ct);
            if (p is null) return new();

            var ids = new HashSet<int>(new[] { p.BenutzerId, p.ProjektleiterId, p.AuftraggeberId });
            var list = await _db.Benutzer.AsNoTracking().Where(b => ids.Contains(b.Id)).OrderBy(b => b.Email).ToListAsync(ct);
            return list;
        }

        public Task<List<Benutzer>> GetAlleBenutzerAsync(CancellationToken ct = default)
            => _db.Benutzer.AsNoTracking().OrderBy(b => b.Email).ToListAsync(ct);
    }
}
