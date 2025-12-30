using ProActive2508.Models.Lisa;

namespace ProActive2508.Service
{
    public class Umfrage
    {
        public List<ProjektUmfrage> GetKat()
        {
            return new List<ProjektUmfrage>
            {
                new ProjektUmfrage
                {
                     Name = "Allgemeine Projektzufriedenheit",
                     Questions = new List<Frage>
                     {
                    new Frage { Text = "Wie zufrieden bist du insgesamt mit dem Projektverlauf?" },
                    new Frage { Text = "Wie zufrieden bist du mit dem Endergebnis des Projekts?" },
                    new Frage { Text = "Wie klar waren die Projektziele für dich?" },
                    new Frage { Text = "Wie gut war die Planung des Projekts?" }
                     }

                },
                 new ProjektUmfrage
                {
                    Name = "Zusammenarbeit & Teamdynamik",
                    Questions = new List<Frage>
                    {
                        new Frage { Text = "Wie gut hat die Zusammenarbeit im Projektteam funktioniert?" },
                        new Frage { Text = "Wie gut war die Kommunikation zwischen den Teammitgliedern?" },
                        new Frage { Text = "Wie wertgeschätzt hast du dich im Projekt gefühlt?" },
                        new Frage { Text = "Wie gut wurden Konflikte im Team gelöst?" }
                    }
                },

                new ProjektUmfrage
                {
                    Name = "Führung & Projektleitung",
                    Questions = new List<Frage>
                    {
                        new Frage { Text = "Wie gut war die Kommunikation mit der Projektleitung?" },
                        new Frage { Text = "Wie klar waren die Anweisungen und Erwartungen?" },
                        new Frage { Text = "Wie gut wurden Entscheidungen getroffen und kommuniziert?" },
                        new Frage { Text = "Wie fair wurden Aufgaben verteilt?" }
                    }
                },
                 new ProjektUmfrage
                {
                    Name = "Prozesse & Abläufe",
                    Questions = new List<Frage>
                    {
                        new Frage { Text = "Wie gut haben die Arbeitsprozesse funktioniert?" },
                        new Frage { Text = "Wie gut war die Materialversorgung organisiert?" },
                        new Frage { Text = "Wie gut haben Schnittstellen zu anderen Abteilungen funktioniert?" },
                        new Frage { Text = "Wie realistisch waren die Zeitpläne?" }
                    }
                },

                new ProjektUmfrage
                {
                    Name = "Belastung & Arbeitsbedingungen",
                    Questions = new List<Frage>
                    {
                        new Frage { Text = "Wie hoch war deine Arbeitsbelastung während des Projekts?" },
                        new Frage { Text = "Wie gut konntest du Pausen einhalten?" },
                        new Frage { Text = "Wie gut war die körperliche Belastung tragbar?" },
                        new Frage { Text = "Wie gut war die mentale Belastung tragbar?" }
                    }
                }

            };
        }
    }
}
