//// Datei: Components/Pages/Anja/ProjektDetails.razor.cs
//// Seite: ProjektDetails (Code-Behind)

//using Microsoft.AspNetCore.Components;
//using ProActive2508.Models.Entity.Anja;
//using ProActive2508.Service;

//namespace ProActive2508.Components.Pages.Anja
//{
//    public partial class ProjektDetails : ComponentBase
//    {
//        [Parameter] public int Id { get; set; }

//        [Inject] public IProjekteService ProjekteService { get; set; } = default!;
//        [Inject] public IAufgabenService AufgabenService { get; set; } = default!;

//        protected bool isLoading = true;
//        protected Projekt? projekt;
//        protected List<Aufgabe> aufgaben = new();
//        protected Dictionary<int, string> userLookup = new();

//        protected override async Task OnParametersSetAsync()
//        {
//            isLoading = true;

//            // Projekt & Anzeige-Lookups
//            var alleBenutzer = await ProjekteService.GetAlleBenutzerAsync();
//            userLookup = alleBenutzer.ToDictionary(b => b.Id, b => string.IsNullOrWhiteSpace(b.Email) ? $"User #{b.Id}" : b.Email);

//            var alleProjekte = await ProjekteService.GetAllAsync();
//            projekt = alleProjekte.FirstOrDefault(p => p.Id == Id);

//            // Aufgaben dieses Projekts laden (falls Service-Methode vorhanden: GetByProjektIdAsync verwenden)
//            var alleAufgaben = await AufgabenService.GetByProjektIdAsync(Id);
//            aufgaben = alleAufgaben.OrderBy(a => a.Faellig).ToList();

//            isLoading = false;
//        }

//        protected string UserName(int id) => userLookup.TryGetValue(id, out var n) ? n : $"User #{id}";

//        protected string Titel(Projekt p)
//            => string.IsNullOrWhiteSpace(p.Projektbeschreibung) ? $"Projekt #{p.Id}" : p.Projektbeschreibung;
//    }
//}
