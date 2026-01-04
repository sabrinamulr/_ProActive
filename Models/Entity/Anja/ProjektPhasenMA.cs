using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class ProjektPhasenMA
    {
        [Key]
        public int Id { get; set; }

        public int BenutzerId { get; set; }

        public int ProjektphasenId { get; set; }

        public string? Rolle { get; set; }

        public string? Zustandigkeit { get; set; }

        [ForeignKey(nameof(BenutzerId))]
        [InverseProperty(nameof(Benutzer.ProjektPhasenMitarbeiter))]
        public Benutzer? Benutzer { get; set; }

        [ForeignKey(nameof(ProjektphasenId))]
        [InverseProperty(nameof(ProjektPhase.ProjektPhasenMitarbeiter))]
        public ProjektPhase? ProjektPhase { get; set; }
    }
}
