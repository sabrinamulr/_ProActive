using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class ProjektPhase
    {
        [Key]
        public int Id { get; set; }

        public int ProjekteId { get; set; }

        public int PhasenId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? Abschlussdatum { get; set; }

        public string? Status { get; set; }

        public int VerantwortlicherbenutzerId { get; set; }

        public string? Notizen { get; set; }

        [ForeignKey(nameof(ProjekteId))]
        [InverseProperty(nameof(Projekt.ProjektPhasen))]
        public Projekt? Projekt { get; set; }

        [ForeignKey(nameof(PhasenId))]
        [InverseProperty(nameof(Phase.ProjektPhasen))]
        public Phase? Phase { get; set; }

        [ForeignKey(nameof(VerantwortlicherbenutzerId))]
        [InverseProperty(nameof(Benutzer.ProjektPhasenAlsVerantwortlicher))]
        public Benutzer? VerantwortlicherBenutzer { get; set; }

        [InverseProperty(nameof(PhaseMeilenstein.ProjektPhase))]
        public ICollection<PhaseMeilenstein> PhaseMeilensteine { get; set; } = new List<PhaseMeilenstein>();

        [InverseProperty(nameof(ProjektPhasenMA.ProjektPhase))]
        public ICollection<ProjektPhasenMA> ProjektPhasenMitarbeiter { get; set; } = new List<ProjektPhasenMA>();
    }
}
