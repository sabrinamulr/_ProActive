// Datei: Service/IAufgabenService.cs
// Seite: IAufgabenService

using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Service
{
    public interface IAufgabenService
    {
        Task<List<Aufgabe>> GetOffeneFuerBenutzerAsync(int benutzerId, bool includeDone = false, CancellationToken ct = default);
        Task<List<Aufgabe>> GetZuweisungenVonLeiterAsync(int projektleiterId, CancellationToken ct = default);
        Task<Aufgabe?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Aufgabe> CreateAsync(Aufgabe neu, int currentUserId, bool isProjektleiter, CancellationToken ct = default);
        Task<bool> UpdateAsync(Aufgabe changed, int currentUserId, bool isProjektleiter, CancellationToken ct = default);
        Task<List<Aufgabe>> GetByProjektIdAsync(int projektId);
    }
}
