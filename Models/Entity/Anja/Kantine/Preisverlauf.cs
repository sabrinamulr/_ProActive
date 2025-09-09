using System.ComponentModel.DataAnnotations;

namespace ProActive2508.Models.Entity.Anja.Kantine
{
    public class Preisverlauf
    {
        public int Id { get; set; }

        public int GerichtId { get; set; }
        public Gericht Gericht { get; set; } = default!;

        [Range(0, 100000)]
        public decimal Preis { get; set; }

        [DataType(DataType.Date)]
        public DateTime GueltigAb { get; set; } = DateTime.Today;
    }
}

