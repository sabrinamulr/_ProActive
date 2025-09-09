//// Datei: Components/Pages/Anja/MeineProjekte.razor.cs
//// Seite: MeineProjekte (Code-Behind)

//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Components.Authorization;
//using ProActive2508.Models.Entity.Anja;
//using ProActive2508.Service;
//using System.Security.Claims;

//namespace ProActive2508.Components.Pages.Anja
//{
//	public partial class MeineProjekte : ComponentBase
//	{
//		[Inject] public IProjekteService ProjekteService { get; set; } = default!;
//		[Inject] public AuthenticationStateProvider Auth { get; set; } = default!;

//		protected bool isLoading = true;
//		protected int currentUserId;
//		protected List<Projekt> meineProjekte = new();

//		protected override async Task OnInitializedAsync()
//		{
//			var auth = await Auth.GetAuthenticationStateAsync();
//			var user = auth.User;
//			currentUserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

//			var alle = await ProjekteService.GetAllAsync();
//			meineProjekte = alle
//				.Where(p => p.BenutzerId == currentUserId || p.ProjektleiterId == currentUserId)
//				.OrderBy(p => p.Id)
//				.ToList();

//			isLoading = false;
//		}

//		protected string Titel(Projekt p)
//			=> string.IsNullOrWhiteSpace(p.Projektbeschreibung) ? $"Projekt #{p.Id}" : p.Projektbeschreibung;

//		protected string RolleFür(Projekt p)
//		{
//			var owner = p.BenutzerId == currentUserId;
//			var leiter = p.ProjektleiterId == currentUserId;
//			if (owner && leiter) return "Owner & Projektleiter";
//			if (owner) return "Owner";
//			if (leiter) return "Projektleiter";
//			return "-";
//		}
//	}
//}
