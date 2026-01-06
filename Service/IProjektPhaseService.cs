namespace ProActive2508.Service
{
    public interface IProjektPhaseService
    {
        /// <summary>
        /// Prüft ob der PhaseMeilenstein der angegebenen ProjektPhase den Status "freigegeben" hat.
        /// Falls ja: markiert die aktuelle ProjektPhase als abgeschlossen (Abschlussdatum) und
        /// setzt die nächste ProjektPhase als aktiv (nur durch Abschluss/Datum gesteuert).
        /// Liefert true, wenn ein Gate‑Exit durchgeführt wurde.
        /// </summary>
        Task<bool> TryAdvancePhaseAsync(int projectId, int projektPhaseId, int performedByUserId, CancellationToken ct = default);
    }
}