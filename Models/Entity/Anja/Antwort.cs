// Seite: Antwort (Entity/Models)
// Datei: Models/Entity/Anja/Antwort.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Antwort
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FrageId { get; set; }

        [Required]
        public int ProjektId { get; set; }

        [Required]
        public int BenutzerId { get; set; }

        public int Rating { get; set; }

        [DataType(DataType.Date)]
        public DateTime Datum { get; set; }

        // --- Navigationen ---
        [ForeignKey(nameof(FrageId))]
        [InverseProperty(nameof(Frage.Antworten))]
        public Frage? Frage { get; set; }

        [ForeignKey(nameof(ProjektId))]
        [InverseProperty(nameof(Projekt.Antworten))]
        public Projekt? Projekt { get; set; }

        [ForeignKey(nameof(BenutzerId))]
        [InverseProperty(nameof(Benutzer.Antworten))]
        public Benutzer? Benutzer { get; set; }
    }
}
