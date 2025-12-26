// Datei: Models/Entity/Anja/ProjektBenutzer.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace ProActive2508.Models.Entity.Anja
{
    public class ProjektBenutzer
    {
        public int ProjektId { get; set; }
        public int BenutzerId { get; set; }

        [ForeignKey(nameof(ProjektId))]
        public Projekt? Projekt { get; set; }

        [ForeignKey(nameof(BenutzerId))]
        public Benutzer? Benutzer { get; set; }
    }
}
