using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Phase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Bezeichnung { get; set; } = string.Empty;

        [Required]
        public string Kurzbezeichnung { get; set; } = string.Empty;

        [InverseProperty(nameof(ProjektPhase.Phase))]
        public ICollection<ProjektPhase> ProjektPhasen { get; set; } = new List<ProjektPhase>();
    }
}
