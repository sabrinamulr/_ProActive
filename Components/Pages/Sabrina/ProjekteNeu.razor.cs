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
            var auth = await Auth.GetAuthenticationStateAsync();
            var user = auth.User;

            var idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst("sub")?.Value;

            int parsed = 0;
            if (!int.TryParse(idClaim, out parsed) || parsed <= 0)
            {
                var name = user.Identity?.Name ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name) && int.TryParse(name, out var pn))
                {
                    var dbUser = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == pn);
                    parsed = dbUser?.Id ?? 0;
                }
                else
                {
                    var dbUserByMail = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Email == name);
                    parsed = dbUserByMail?.Id ?? 0;
                }
            }

            CurrentUserId = parsed;
        }

        protected async Task SaveAsync()
        {
            uiError = null;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                uiError = "Name ist erforderlich.";
                return;
            }

            isSaving = true;

            try
            {
                var projekt = new Projekt
                {
                    BenutzerId = CurrentUserId,
                    ProjektleiterId = CurrentUserId,
                    AuftraggeberId = CurrentUserId,
                    Projektbeschreibung = model.Name + (string.IsNullOrWhiteSpace(model.Description) ? string.Empty : " — " + model.Description),
                    Status = 0, // bestehende DB erwartet int/enum-Konvertierung
                    Phase = 0
                };

                Db.Projekte.Add(projekt);
                await Db.SaveChangesAsync();

                Nav.NavigateTo("/meine-projekte");
            }
            catch (Exception ex)
            {
                uiError = ex.InnerException?.Message ?? ex.Message;
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