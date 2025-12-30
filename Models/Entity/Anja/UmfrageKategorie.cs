// Seite: UmfrageKategorie (Entity/Models)
// Datei: Models/Entity/Anja/UmfrageKategorie.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class UmfrageKategorie
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // --- Navigationen ---
        [InverseProperty(nameof(Frage.Kategorie))]
        public ICollection<Frage> Fragen { get; set; } = new List<Frage>();
    }
}
