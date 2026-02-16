namespace ProActive2508.Service
{
    public interface IProjektPhaseService
    {
        Task<bool> TryAdvancePhaseAsync(int projectId, int projektPhaseId, int performedByUserId, CancellationToken ct = default);
    }
}