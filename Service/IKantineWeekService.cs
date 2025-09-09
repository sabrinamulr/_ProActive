// Datei: Service/IKantineWeekService.cs
// Seite: IKantineWeekService

using ProActive2508.Models.Entity.Anja.Kantine;

namespace ProActive2508.Service
{
    public record WeekRowPayload { public DateTime Tag { get; init; } public string Menu1 { get; init; } = string.Empty; public string Menu1Allergene { get; init; } = string.Empty; public decimal? Menu1Preis { get; init; } public string Menu2 { get; init; } = string.Empty; public string Menu2Allergene { get; init; } = string.Empty; public decimal? Menu2Preis { get; init; } }
    public record GerichtInfo(int Id, string Name, string Allergene, decimal? LastPrice);

    public interface IKantineWeekService
    {
        (DateTime Monday, DateTime Friday) GetWeekRange(DateTime reference, int offsetWeeks);
        Task<bool> WeekHasPlanAsync(int offsetWeeks, CancellationToken ct = default);
        Task<List<MenueplanTag>> LoadWeekAsync(int offsetWeeks, CancellationToken ct = default);
        Task SaveWeekAsync(int offsetWeeks, List<WeekRowPayload> payload, CancellationToken ct = default);
        Task<GerichtInfo?> FindGerichtInfoAsync(string name, CancellationToken ct = default);
        Task EnsurePreisForGerichtAsync(string gerichtName, decimal? desiredPrice, CancellationToken ct = default);

        // ▼▼▼ NEU: Allergene anhand der Codes (z. B. "A, C, G") setzen – alte Verknüpfungen werden gelöscht
        Task UpdateAllergeneForGerichtAsync(string gerichtName, string allergeneCodes, CancellationToken ct = default);
    }
}
