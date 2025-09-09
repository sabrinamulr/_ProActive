// Seite: Aufgabe (Entity/Models)
// Datei: Components/Entity/Aufgabe.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class Aufgabe
    {
        // Primärschlüssel – immer in der Entity definiert
        [Key]
        public int Id { get; set; }

        // FK (optional): Zu welchem Projekt gehört die Aufgabe?
        // NULL = kein Projekt ausgewählt
        public int? ProjektId { get; set; }

        // FK: Wer ist zuständig?
        [Required]
        public int BenutzerId { get; set; }

        // Kurzbeschreibung der Aufgabe (Pflicht, max. 500 Zeichen)
        [Required, MaxLength(500)]
        public string Aufgabenbeschreibung { get; set; } = string.Empty;

        // Fälligkeitsdatum (Pflicht)
        [Required]
        [DataType(DataType.Date)]
        public DateTime Faellig { get; set; }

        // Phase als einfache Zahl (falls ihr später Enum wollt → austauschen)
        public int Phase { get; set; }

        // Status (offen / in Bearbeitung / erledigt)
        [Required]
        public Erledigungsstatus Erledigt { get; set; } = Erledigungsstatus.Offen;

        // Wer hat die Aufgabe erstellt?
        [Required]
        public int ErstellVon { get; set; }

        // --- Navigationen ---
        [ForeignKey(nameof(ProjektId))]
        [InverseProperty(nameof(Projekt.Aufgaben))]
        public Projekt? Projekt { get; set; }

        [ForeignKey(nameof(BenutzerId))]
        [InverseProperty(nameof(Benutzer.Aufgaben))]
        public Benutzer? Benutzer { get; set; }
    }

    // Feinere Stadien der Bearbeitung
    public enum Erledigungsstatus
    {
        Offen = 0,
        InBearbeitung = 1,
        Erledigt = 2,
    }
}
