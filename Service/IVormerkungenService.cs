// Datei: Service/IVormerkungService.cs
// Seite: IVormerkungService

namespace ProActive2508.Service
{
    public enum VormerkAktion { Loeschen = 1, MarkErledigt = 2 }

    public interface IVormerkungService
    {
        Task<int> VormerkenAsync(int aufgabeId, int ausloeserId, VormerkAktion aktion, TimeSpan? delay = null, CancellationToken ct = default);
        Task<bool> UndoAsync(int vormerkId, int currentUserId, CancellationToken ct = default);
        Task<int> ProcessDueAsync(CancellationToken ct = default);                                       // vom Worker oder beim Laden „aufholen“
    }
}
