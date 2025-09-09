// Datei: Components/Entity/Projekte.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Projekt
    {
        [Key]
        public int Id { get; set; }

        // Optional: "Hauptaufgabe" (separate Referenz auf eine Aufgabe)
        public int? AufgabeId { get; set; }

        // Eigentümer/Owner des Projekts
        [Required]
        public int BenutzerId { get; set; }

        [Required]
        public Projektstatus Status { get; set; } = Projektstatus.Aktiv;

        [Required]
        public int ProjektleiterId { get; set; }

        [Required]
        public int AuftraggeberId { get; set; }

        [Required]
        public Projektphase Phase { get; set; } = Projektphase.Initialisierung;

        public int? ProblemId { get; set; }

        [Required]
        public string Projektbeschreibung { get; set; } = string.Empty;

        // --- Navigationen ---
        [ForeignKey(nameof(BenutzerId))]
        [InverseProperty(nameof(Benutzer.ProjekteAlsOwner))]
        public Benutzer? Owner { get; set; }

        [ForeignKey(nameof(ProjektleiterId))]
        [InverseProperty(nameof(Benutzer.ProjekteAlsProjektleiter))]
        public Benutzer? Projektleiter { get; set; }

        [ForeignKey(nameof(AuftraggeberId))]
        [InverseProperty(nameof(Benutzer.ProjekteAlsAuftraggeber))]
        public Benutzer? Auftraggeber { get; set; }

        // 1:n Projekt -> Aufgaben
        [InverseProperty(nameof(Aufgabe.Projekt))]
        public ICollection<Aufgabe> Aufgaben { get; set; } = new List<Aufgabe>();

    }

    public enum Projektstatus
    {
        Aktiv = 0,
        Pausiert = 1,
        Abgeschlossen = 2,
        Archiviert = 3
    }

    public enum Projektphase
    {
        Initialisierung = 0,
        Planung = 1,
        Umsetzung = 2,
        Abnahme = 3
    }
}
