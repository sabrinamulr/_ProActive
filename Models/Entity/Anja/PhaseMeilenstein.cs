using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class PhaseMeilenstein
    {
        [Key]
        public int Id { get; set; }

        public int ProjektphasenId { get; set; }

        public int MeilensteinId { get; set; }

        public string? Status { get; set; }

        public DateTime Zieldatum { get; set; }

        public DateTime? Erreichtdatum { get; set; }

        public int GenehmigerbenutzerId { get; set; }

        [ForeignKey(nameof(ProjektphasenId))]
        [InverseProperty(nameof(ProjektPhase.PhaseMeilensteine))]
        public ProjektPhase? ProjektPhase { get; set; }

        [ForeignKey(nameof(MeilensteinId))]
        [InverseProperty(nameof(Meilenstein.PhaseMeilensteine))]
        public Meilenstein? Meilenstein { get; set; }

        [ForeignKey(nameof(GenehmigerbenutzerId))]
        [InverseProperty(nameof(Benutzer.GenehmigtePhaseMeilensteine))]
        public Benutzer? GenehmigerBenutzer { get; set; }
    }
}
