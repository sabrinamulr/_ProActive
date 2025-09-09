// Datei: Service/IMenuplanPdfService.cs
// Seite: IMenuplanPdfService
namespace ProActive2508.Service
{
    public interface IMenuplanPdfService
    {
        Task<byte[]> BuildMenuplanPdfAsync(
            DateTime monday, DateTime friday,
            string? kantinenName,
            string title,
            IEnumerable<PdfMenuDay> days,
            decimal? mealPrice,
            string? logoPath = null);
    }

    public sealed class PdfMenuDay
    {
        public DateTime Date { get; set; }
        public string Menu1 { get; set; } = string.Empty;
        public string Menu1Allergens { get; set; } = string.Empty;
        public string Menu2 { get; set; } = string.Empty;
        public string Menu2Allergens { get; set; } = string.Empty;
    }
}