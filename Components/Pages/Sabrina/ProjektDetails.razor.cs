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
    public partial class ProjektDetails : ComponentBase
    {
        [Parameter] public int Id { get; set; }

        protected Projekt? project;
        protected Projekt? editModel;
        protected bool isLoading = true;
        protected bool isEditing = false;
        protected bool CanEdit = false;
        protected Dictionary<int, string> _userLookup = new();

        [Inject] private AppDbContext Db { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            isLoading = true;
            try
            {
                var auth = await AuthenticationStateTask;
                var user = auth.User;

                var idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;
                int currentUserId = 0;
                if (!int.TryParse(idClaim, out currentUserId) || currentUserId <= 0)
                {
                    var name = user.Identity?.Name ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(name) && int.TryParse(name, out var pn))
                    {
                        var dbUser = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == pn);
                        currentUserId = dbUser?.Id ?? 0;
                    }
                    else
                    {
                        var dbUserByMail = await Db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Email == name);
                        currentUserId = dbUserByMail?.Id ?? 0;
                    }
                }

                project = await Db.Projekte.AsNoTracking().FirstOrDefaultAsync(p => p.Id == Id);
                if (project is null) return;

                var userIds = new[] { project.ProjektleiterId, project.AuftraggeberId }.Where(i => i > 0).Distinct().ToList();
                var users = await Db.Benutzer.AsNoTracking().Where(b => userIds.Contains(b.Id)).Select(b => new { b.Id, b.Email }).ToListAsync();
                _userLookup = users.ToDictionary(x => x.Id, x => string.IsNullOrWhiteSpace(x.Email) ? $"User#{x.Id}" : x.Email);

                // Berechtigung prüfen: Rolle Projektleiter oder spezifischer Projektleiter
                var isRoleProjektleiter = user.IsInRole("Projektleiter");
                CanEdit = isRoleProjektleiter || project.ProjektleiterId == currentUserId;
            }
            catch
            {
                project = null;
            }
            finally
            {
                isLoading = false;
            }
        }

        protected void EnableEdit()
        {
            if (project is null) return;
            editModel = new Projekt
            {
                Id = project.Id,
                Projektbeschreibung = project.Projektbeschreibung,
                BenutzerId = project.BenutzerId,
                ProjektleiterId = project.ProjektleiterId,
                AuftraggeberId = project.AuftraggeberId,
                Status = project.Status,
                Phase = project.Phase
            };
            isEditing = true;
        }

        protected async Task SaveAsync()
        {
            if (editModel is null) return;
            // Serverseitige Berechtigungsprüfung nicht nochmals gezeigt, aber empfohlen
            try
            {
                Db.Projekte.Update(editModel);
                await Db.SaveChangesAsync();
                isEditing = false;
                await LoadAsync();
            }
            catch (Exception)
            {
                // Fehlerbehandlung nach Bedarf

            }
        }

        protected void CancelEdit()
        {
            isEditing = false;
            editModel = null;
        }
    }
}