namespace ProActive2508.Models.Entity.Anja.Kantine
{
    public class GerichtAllergen
    {
        public int GerichtId { get; set; }
        public Gericht Gericht { get; set; } = default!;

        public int AllergenId { get; set; }
        public Allergen Allergen { get; set; } = default!;
    }
}
