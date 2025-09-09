// Seite: Vorbestellung (Entity/Models)
// Datei: Models/Entity/Anja/Kantine/Vorbestellung.cs

using System;
using System.ComponentModel.DataAnnotations;                // [Key], [Required]
using Microsoft.EntityFrameworkCore;                       // [Index]
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Models.Entity.Anja.Kantine
{
    // GENAU EINE Vormerkung pro Benutzer und Tag:
    [Index(nameof(BenutzerId), nameof(MenueplanTagId), IsUnique = true)]

    // Optionaler zusätzlicher „Sicherheitsgurt“:
    // Verhindert, dass derselbe Benutzer denselben Eintrag (Menueplan.Id) doppelt vormerkt.
    [Index(nameof(BenutzerId), nameof(EintragId), IsUnique = true)]
    public class Vorbestellung
    {
        [Key]                                              // Primary Key IMMER in der Entity
        public int Id { get; set; }

        [Required]
        public int BenutzerId { get; set; }
        public Benutzer Benutzer { get; set; } = default!;

        [Required]
        public int MenueplanTagId { get; set; }
        public MenueplanTag MenueplanTag { get; set; } = default!;

        [Required]                                         // gewählte Menü-Position (Menueplan.Id)
        public int EintragId { get; set; }
        public Menueplan Eintrag { get; set; } = default!;

        public DateTime BestelltAm { get; set; } = DateTime.UtcNow;
    }
}
