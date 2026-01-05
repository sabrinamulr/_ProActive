using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Components.Pages.Sabrina
{
    public partial class ProjekteNeu : ComponentBase
    {
        protected NewProjectModel model = new();
        protected bool isSaving = false;
        protected string? uiError;

        [Inject] private AppDbContext Db { get; set; } = default!;
        [Inject] private AuthenticationStateProvider Auth { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        private int CurrentUserId;

        protected override async Task OnInitializedAsync()
        {
            AuthenticationState auth = await Auth.GetAuthenticationStateAsync();
            System.Security.Claims.ClaimsPrincipal user = auth.User;

            string? idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value;

            int parsed = 0;
            if (!int.TryParse(idClaim, out parsed) || parsed <= 0)
            {
                string? name = user.Identity?.Name ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name) && int.TryParse(name, out int pn))
                {
                    Benutzer? dbUser = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == pn);
                    parsed = dbUser?.Id ?? 0;
                }
                else
                {
                    Benutzer? dbUserByMail = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Email == name);
                    parsed = dbUserByMail?.Id ?? 0;
                }
            }

            CurrentUserId = parsed;
        }

        // Standard-Save (Form-Submit)
        protected async Task SaveAsync()
        {
            await SaveCoreAsync(redirectToPhases: false);
        }

        // Aufruf durch "Erstellen & Phasen definieren"
        protected async Task SaveAndDefineAsync()
        {
            await SaveCoreAsync(redirectToPhases: true);
        }

        // Core-Create: gibt bei Erfolg neue Projekt-Id zurück und navigiert je nach Flag
        private async Task<int> SaveCoreAsync(bool redirectToPhases)
        {
            uiError = null;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                uiError = "Name ist erforderlich.";
                return 0;
            }

            isSaving = true;

            try
            {
                Projekt projekt = new Projekt
                {
                    BenutzerId = CurrentUserId,
                    ProjektleiterId = CurrentUserId,
                    AuftraggeberId = CurrentUserId,
                    Projektbeschreibung = model.Name + (string.IsNullOrWhiteSpace(model.Description) ? string.Empty : " — " + model.Description),
                    Status = Projektstatus.Aktiv,
                    Phase = Projektphase.Initialisierung
                };

                Db.Projekte.Add(projekt);
                await Db.SaveChangesAsync();

                int newId = projekt.Id;

                if (redirectToPhases)
                {
                    Nav.NavigateTo($"/projekt/{newId}/phasen-definieren");
                }
                else
                {
                    Nav.NavigateTo("/meine-projekte");
                }

                return newId;
            }
            catch (Exception ex)
            {
                uiError = ex.InnerException?.Message ?? ex.Message;
                return 0;
            }
            finally
            {
                isSaving = false;
            }
        }

        protected void Cancel()
        {
            Nav.NavigateTo("/");
        }

        protected class NewProjectModel
        {
            [System.ComponentModel.DataAnnotations.Required]
            public string Name { get; set; } = string.Empty;

            public string? Description { get; set; }

            public DateTime? ExpectedCompletionDate { get; set; }
        }
    }
}