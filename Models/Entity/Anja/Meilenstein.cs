using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Meilenstein
    {
        [Key]
        public int Id { get; set; }

        public int ProjektphasenId { get; set; }

        [Required]
        public string Bezeichnung { get; set; } = string.Empty;

        public string? Status { get; set; }

        public DateTime Zieldatum { get; set; }

        public DateTime? Erreichtdatum { get; set; }

        public int GenehmigerbenutzerId { get; set; }

        [ForeignKey(nameof(ProjektphasenId))]
        [InverseProperty(nameof(ProjektPhase.Meilensteine))]
        public ProjektPhase? ProjektPhase { get; set; }

        [ForeignKey(nameof(GenehmigerbenutzerId))]
        [InverseProperty(nameof(Benutzer.GenehmigteMeilensteine))]
        public Benutzer? GenehmigerBenutzer { get; set; }
    }
}
