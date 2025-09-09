using System.ComponentModel.DataAnnotations;

namespace ProActive2508.Models.Entity.Anja.Kantine
{
    public class Allergen
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(10)]
        public string Kuerzel { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Bezeichnung { get; set; } = string.Empty;

        public ICollection<GerichtAllergen> GerichtAllergene { get; set; } = new List<GerichtAllergen>();
    }
}
