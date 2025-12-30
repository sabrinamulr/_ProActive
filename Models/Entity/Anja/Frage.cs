// Seite: Frage (Entity/Models)
// Datei: Models/Entity/Anja/Frage.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Frage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KategorieId { get; set; }

        public string? Text { get; set; }

        // --- Navigationen ---
        [ForeignKey(nameof(KategorieId))]
        [InverseProperty(nameof(UmfrageKategorie.Fragen))]
        public UmfrageKategorie? Kategorie { get; set; }

        [InverseProperty(nameof(Antwort.Frage))]
        public ICollection<Antwort> Antworten { get; set; } = new List<Antwort>();
    }
}
