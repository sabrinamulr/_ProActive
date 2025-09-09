using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja.Kantine;
using System.Globalization;


namespace ProActive2508.Service
{
    public class KantineWeekService : IKantineWeekService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public KantineWeekService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

        public (DateTime Monday, DateTime Friday) GetWeekRange(DateTime reference, int offsetWeeks)
        {
            var today = reference.Date;
            int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var monday = today.AddDays(-diff).AddDays(7 * offsetWeeks);
            var friday = monday.AddDays(4);
            return (monday, friday);
        }

        public async Task<bool> WeekHasPlanAsync(int offsetWeeks, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (mo, fr) = GetWeekRange(DateTime.Today, offsetWeeks);
            return await db.Menueplaene
                .AsNoTracking()
                .Include(m => m.MenueplanTag)
                .AnyAsync(m => m.MenueplanTag.Tag >= mo && m.MenueplanTag.Tag <= fr, ct);
        }

        public async Task<List<MenueplanTag>> LoadWeekAsync(int offsetWeeks, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var (mo, fr) = GetWeekRange(DateTime.Today, offsetWeeks);
            return await db.MenueplanTage
                .AsNoTracking()
                .Where(t => t.Tag >= mo && t.Tag <= fr)
                .Include(t => t.Eintraege).ThenInclude(e => e.Gericht)
                .OrderBy(t => t.Tag)
                .ToListAsync(ct);
        }

        public async Task SaveWeekAsync(int offsetWeeks, List<WeekRowPayload> payload, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Doppelte Anlage verhindern
            var (mo, fr) = GetWeekRange(DateTime.Today, offsetWeeks);
            bool exists = await db.Menueplaene
                .Include(m => m.MenueplanTag)
                .AnyAsync(m => m.MenueplanTag.Tag >= mo && m.MenueplanTag.Tag <= fr, ct);
            if (exists) throw new InvalidOperationException("Für diese Woche existieren bereits Einträge.");

            foreach (var row in payload)
            {
                // Tag upsert
                var tag = await db.MenueplanTage.FirstOrDefaultAsync(x => x.Tag == row.Tag, ct);
                if (tag is null)
                {
                    tag = new MenueplanTag { Tag = row.Tag, Woche = ISOWeek.GetWeekOfYear(row.Tag) };
                    db.MenueplanTage.Add(tag);
                    await db.SaveChangesAsync(ct);
                }

                // Menü 1
                if (!string.IsNullOrWhiteSpace(row.Menu1))
                {
                    var g1 = await GetOrCreateGerichtAsync(db, row.Menu1, ct);
                    await UpdateAllergeneAsync(db, g1, row.Menu1Allergene, ct);
                    await EnsurePreisAsync(db, g1.Id, row.Menu1Preis, ct);
                    await UpsertMenuAsync(db, tag.Id, 1, g1.Id, ct);
                }
                // Menü 2
                if (!string.IsNullOrWhiteSpace(row.Menu2))
                {
                    var g2 = await GetOrCreateGerichtAsync(db, row.Menu2, ct);
                    await UpdateAllergeneAsync(db, g2, row.Menu2Allergene, ct);
                    await EnsurePreisAsync(db, g2.Id, row.Menu2Preis, ct);
                    await UpsertMenuAsync(db, tag.Id, 2, g2.Id, ct);
                }
            }
        }

        public async Task<GerichtInfo?> FindGerichtInfoAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cleaned = name.Trim().ToLower();
            var gericht = await db.Gerichte.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Gerichtname.ToLower() == cleaned, ct);
            if (gericht is null) return null;

            var allergene = await db.GerichtAllergene
                .AsNoTracking()
                .Where(ga => ga.GerichtId == gericht.Id)
                .Include(ga => ga.Allergen)
                .Select(ga => ga.Allergen.Kuerzel)
                .ToListAsync(ct);

            var lastPrice = await db.Preisverlaeufe
                .AsNoTracking()
                .Where(p => p.GerichtId == gericht.Id)
                .OrderByDescending(p => p.GueltigAb)
                .Select(p => (decimal?)p.Preis)
                .FirstOrDefaultAsync(ct);

            return new GerichtInfo(gericht.Id, gericht.Gerichtname, string.Join(", ", allergene), lastPrice);
        }

        // --- helpers (benutzen jeweils denselben db wie der Aufrufer) ---

        private static async Task<Gericht> GetOrCreateGerichtAsync(AppDbContext db, string name, CancellationToken ct)
        {
            var cleaned = name.Trim();
            var g = await db.Gerichte.FirstOrDefaultAsync(x => x.Gerichtname.ToLower() == cleaned.ToLower(), ct);
            if (g is null)
            {
                g = new Gericht { Gerichtname = cleaned };
                db.Gerichte.Add(g);
                await db.SaveChangesAsync(ct);
            }
            return g;
        }

        private static async Task UpsertMenuAsync(AppDbContext db, int tagId, byte pos, int gerichtId, CancellationToken ct)
        {
            var existing = await db.Menueplaene.FirstOrDefaultAsync(m => m.MenueplanTagId == tagId && m.PositionNr == pos, ct);
            if (existing is null)
            {
                db.Menueplaene.Add(new Menueplan
                {
                    MenueplanTagId = tagId,
                    PositionNr = pos,
                    GerichtId = gerichtId
                });
            }
            else
            {
                existing.GerichtId = gerichtId;
            }
            await db.SaveChangesAsync(ct);
        }

        private static async Task UpdateAllergeneAsync(AppDbContext db, Gericht gericht, string tokens, CancellationToken ct)
        {
            var kuerzels = (tokens ?? string.Empty)
                .ToUpper()
                .Split(new[] { ' ', ',', ';', '/', '\\', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();

            var currentIds = await db.GerichtAllergene
                .Where(ga => ga.GerichtId == gericht.Id)
                .Select(ga => ga.AllergenId)
                .ToListAsync(ct);
            var current = currentIds.ToHashSet();

            var wantedIds = await db.Allergene
                .Where(a => kuerzels.Contains(a.Kuerzel.ToUpper()))
                .Select(a => a.Id)
                .ToListAsync(ct);
            var wanted = wantedIds.ToHashSet();

            // entfernen
            var toRemove = current.Except(wanted).ToList();
            if (toRemove.Count > 0)
            {
                db.GerichtAllergene.RemoveRange(
                    db.GerichtAllergene.Where(ga => ga.GerichtId == gericht.Id && toRemove.Contains(ga.AllergenId))
                );
            }
            // hinzufügen
            var toAdd = wanted.Except(current).Select(id => new GerichtAllergen { GerichtId = gericht.Id, AllergenId = id });
            await db.GerichtAllergene.AddRangeAsync(toAdd, ct);

            await db.SaveChangesAsync(ct);
        }
        public async Task EnsurePreisForGerichtAsync(string gerichtName, decimal? desiredPrice, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(gerichtName)) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cleaned = gerichtName.Trim();
            var gericht = await db.Gerichte.AsNoTracking().FirstOrDefaultAsync(g => g.Gerichtname.ToLower() == cleaned.ToLower(), ct);
            if (gericht is null) return;                               // Gericht existiert (noch) nicht → nichts tun

            await EnsurePreisAsync(db, gericht.Id, desiredPrice, ct);   // nutzt deine bestehende Logik: nur schreiben, wenn geändert
        }

        private static async Task EnsurePreisAsync(AppDbContext db, int gerichtId, decimal? desired, CancellationToken ct)
        {
            // Letzten Preis (inkl. Datum) holen
            var last = await db.Preisverlaeufe
                .Where(p => p.GerichtId == gerichtId)
                .OrderByDescending(p => p.GueltigAb)
                .Select(p => new { p.GueltigAb, p.Preis })
                .FirstOrDefaultAsync(ct);

            decimal? target = desired ?? last?.Preis;
            if (!target.HasValue) return;

            var today = DateTime.Today;

            // 1) Wenn es bereits einen Eintrag für heute gibt -> Update statt Insert
            var todayRow = await db.Preisverlaeufe
                .FirstOrDefaultAsync(p => p.GerichtId == gerichtId && p.GueltigAb == today, ct);

            if (todayRow is not null)
            {
                if (todayRow.Preis != target.Value)
                {
                    todayRow.Preis = target.Value;
                    await db.SaveChangesAsync(ct);
                }
                return;
            }

            // 2) Kein Eintrag für heute: Nur anlegen, wenn sich der Preis zur letzten Historie ändert
            if (last is not null && last.Preis == target.Value)
                return;

            db.Preisverlaeufe.Add(new Preisverlauf
            {
                GerichtId = gerichtId,
                Preis = target.Value,
                GueltigAb = today
            });

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // 3) Race-Condition: jemand anderes hat "heute" gerade eingefügt → auf Update ausweichen
                todayRow = await db.Preisverlaeufe
                    .FirstOrDefaultAsync(p => p.GerichtId == gerichtId && p.GueltigAb == today, ct);

                if (todayRow is not null && todayRow.Preis != target.Value)
                {
                    todayRow.Preis = target.Value;
                    await db.SaveChangesAsync(ct);
                }
            }
        }

        public async Task UpdateAllergeneForGerichtAsync(string gerichtName, string allergeneCodes, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(gerichtName)) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var name = gerichtName.Trim();
            var gericht = await db.Gerichte.AsNoTracking().FirstOrDefaultAsync(g => g.Gerichtname.ToLower() == name.ToLower(), ct);
            if (gericht is null) return; // Gericht existiert noch nicht → wird beim Speichern angelegt

            var codes = ParseAllergenCodes(allergeneCodes); // ["A","C","G",...]
            var allergene = await db.Allergene.Where(a => codes.Contains(a.Kuerzel.ToUpper())).ToListAsync(ct);

            // alle bisherigen Verknüpfungen löschen
            var existing = await db.GerichtAllergene.Where(ga => ga.GerichtId == gericht.Id).ToListAsync(ct);
            db.GerichtAllergene.RemoveRange(existing);

            // neue Verknüpfungen anlegen
            foreach (var a in allergene)
                db.GerichtAllergene.Add(new GerichtAllergen { GerichtId = gericht.Id, AllergenId = a.Id });

            await db.SaveChangesAsync(ct);
        }

        private static List<string> ParseAllergenCodes(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new();
            var parts = raw.Split(new[] { ',', ';', '/', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(p => p.Trim().ToUpperInvariant()).Where(p => p.Length > 0).Distinct().ToList();
        }
    }
}

