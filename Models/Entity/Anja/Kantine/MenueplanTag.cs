using System.ComponentModel.DataAnnotations;

namespace ProActive2508.Models.Entity.Anja.Kantine
{
    public class MenueplanTag
    {
        public int Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime Tag { get; set; }   // UNIQUE

        public int? Woche { get; set; }     // optional

        public ICollection<Menueplan> Eintraege { get; set; } = new List<Menueplan>();
    }
}
