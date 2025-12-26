// Datei: Components/Entity/Benutzer.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Benutzer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Personalnummer { get; set; }

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Stufe { get; set; }

        [MaxLength(100)]
        public string? Abteilung { get; set; }

        [Required]
        public int Verfuegbarkeit { get; set; }

        [Required, MaxLength(200)]
        public string PasswordHash { get; set; } = string.Empty;

       
        // 1:n Benutzer -> Aufgaben (BenutzerId in Aufgabe)
        [InverseProperty(nameof(Aufgabe.Benutzer))]
        public ICollection<Aufgabe> Aufgaben { get; set; } = new List<Aufgabe>();

        // Mehrere Beziehungen Benutzer <-> Projekt müssen eindeutig benannt werden:
        [InverseProperty(nameof(Projekt.Owner))]
        public ICollection<Projekt> ProjekteAlsOwner { get; set; } = new List<Projekt>();

        [InverseProperty(nameof(Projekt.Projektleiter))]
        public ICollection<Projekt> ProjekteAlsProjektleiter { get; set; } = new List<Projekt>();

        [InverseProperty(nameof(Projekt.Auftraggeber))]
        public ICollection<Projekt> ProjekteAlsAuftraggeber { get; set; } = new List<Projekt>();

        // m:n Benutzer <-> Projekt via ProjektBenutzer
        [InverseProperty(nameof(Models.Entity.Anja.ProjektBenutzer.Benutzer))]
        public ICollection<ProjektBenutzer> ProjektBenutzer { get; set; } = new List<ProjektBenutzer>();
    }
}
