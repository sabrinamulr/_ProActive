using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Components.Pages.Sabrina
{
    public partial class ProjekteAnzeigen : ComponentBase
    {
        protected List<Projekt>? projects;
        protected Dictionary<int, string> userLookup = new();
        protected bool isLoading = true;
        protected string? uiError;

        protected bool isModalOpen = false;
        protected string modalTitle = string.Empty;
        protected Projekt? editModel;

        [Inject] private AppDbContext Db { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        private int CurrentUserId;
        private bool isProjektleiterRole;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            uiError = null;

            try
            {
                var auth = await AuthenticationStateTask;
                var user = auth.User;

                isProjektleiterRole = user.IsInRole("Projektleiter");

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

                projects = await Db.Projekte
                    .AsNoTracking()
                    .OrderBy(p => p.Id)
                    .ToListAsync();

                var userIds = projects.SelectMany(p => new[] { p.ProjektleiterId, p.AuftraggeberId })
                                      .Distinct()
                                      .Where(id => id > 0)
                                      .ToList();

                var users = await Db.Benutzer
                    .AsNoTracking()
                    .Where(b => userIds.Contains(b.Id))
                    .Select(b => new { b.Id, b.Email })
                    .ToListAsync();

                userLookup = users.ToDictionary(x => x.Id, x => string.IsNullOrWhiteSpace(x.Email) ? $"User#{x.Id}" : x.Email);
            }
            catch (Exception ex)
            {
                uiError = "Fehler beim Laden: " + (ex.InnerException?.Message ?? ex.Message);
            }
            finally
            {
                isLoading = false;
            }
        }

        protected bool CanEdit(Projekt p)
        {
            // Projektleiter-Rolle oder spezifischer Projektleiter des Projekts darf bearbeiten
            return isProjektleiterRole || p.ProjektleiterId == CurrentUserId;
        }

        protected void OpenEditModal(int projektId)
        {
            editModel = projects?.FirstOrDefault(p => p.Id == projektId);
            if (editModel is null) { uiError = "Projekt nicht gefunden."; return; }

            modalTitle = $"Projekt #{projektId} bearbeiten";
            isModalOpen = true;
        }

        protected void CloseModal()
        {
            isModalOpen = false;
            editModel = null;
        }

        protected async Task SaveEditAsync()
        {
            uiError = null;
            if (editModel is null) return;

            // einfache serverseitige Absicherung: darf der aktuelle Nutzer wirklich das Projekt editieren?
            if (!CanEdit(editModel))
            {
                uiError = "Keine Berechtigung zum Bearbeiten dieses Projekts.";
                return;
            }

            try
            {
                Db.Projekte.Update(editModel);
                await Db.SaveChangesAsync();
                isModalOpen = false;
            }
            catch (Exception ex)
            {
                uiError = "Fehler beim Speichern: " + (ex.InnerException?.Message ?? ex.Message);
            }
            finally
            {
                // Reload list to show changes (optional)
                await OnInitializedAsync();
            }
        }
    }
}