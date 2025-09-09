// Datei: Service/IProjekteService.cs
// Seite: IProjekteService

using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Service
{
    public interface IProjekteService
    {
        Task<List<Projekt>> GetAllAsync(CancellationToken ct = default);
        Task<List<Projekt>> GetProjekteFuerLeiterAsync(int projektleiterId, CancellationToken ct = default);
        Task<List<Benutzer>> GetMitarbeiterFuerProjektAsync(int projektId, bool includeSelf = true, CancellationToken ct = default);
        Task<List<Benutzer>> GetAlleBenutzerAsync(CancellationToken ct = default);
    }
}
