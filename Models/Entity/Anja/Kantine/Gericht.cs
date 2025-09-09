using System.ComponentModel.DataAnnotations;

namespace ProActive2508.Models.Entity.Anja.Kantine
{
    public class Gericht
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Gerichtname { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GerichtAllergen> GerichtAllergene { get; set; } = new List<GerichtAllergen>();
        public ICollection<Preisverlauf> Preisverlaeufe { get; set; } = new List<Preisverlauf>();
    }
}
