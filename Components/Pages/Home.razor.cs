using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;

namespace ProActive2508.Components.Pages
{
    public partial class Home : ComponentBase
    {
        protected List<Projekt>? projects;
        protected bool isLoading = true;

        [Inject] private AppDbContext Db { get; set; } = default!;
        [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            isLoading = true;
            try
            {
                var auth = await AuthenticationStateTask;
                var user = auth.User;

                if (user?.Identity?.IsAuthenticated != true)
                {
                    projects = new List<Projekt>();
                    return;
                }

                // Ermittle aktuellen Benutzer-Id aus Claims (wie im Projekt üblich)
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

                // Lade Projekte, die den Nutzer betreffen (Owner, Projektleiter, Auftraggeber)
                projects = await Db.Projekte
                    .AsNoTracking()
                    .Where(p => p.BenutzerId == currentUserId
                                || p.ProjektleiterId == currentUserId
                                || p.AuftraggeberId == currentUserId)
                    .OrderBy(p => p.Id)
                    .ToListAsync();
            }
            catch
            {
                projects = new List<Projekt>();
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}