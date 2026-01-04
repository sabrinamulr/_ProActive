using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Meilenstein
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Bezeichnung { get; set; } = string.Empty;

        [InverseProperty(nameof(PhaseMeilenstein.Meilenstein))]
        public ICollection<PhaseMeilenstein> PhaseMeilensteine { get; set; } = new List<PhaseMeilenstein>();
    }
}
