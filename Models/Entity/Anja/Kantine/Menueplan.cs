using System.ComponentModel.DataAnnotations;

namespace ProActive2508.Models.Entity.Anja.Kantine
{
    public class Menueplan
    {
        public int Id { get; set; }

        public int MenueplanTagId { get; set; }
        public MenueplanTag MenueplanTag { get; set; } = default!;

        [Range(1, 2)]
        public byte PositionNr { get; set; }   // 1 oder 2

        public int GerichtId { get; set; }
        public Gericht Gericht { get; set; } = default!;
    }
}
