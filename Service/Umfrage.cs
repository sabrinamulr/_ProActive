using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Service
{
    public class Umfrage
    {
        private readonly AppDbContext _context;

        public Umfrage(AppDbContext context)
        {
            _context = context;
        }

        // Holt ALLE Kategorien + Fragen aus der Datenbank
        public async Task<List<UmfrageKategorie>> GetKategorienAsync()
        {
            return await _context.UmfrageKategorien
                .Include(k => k.Fragen)
                .ToListAsync();
        }
    }
}