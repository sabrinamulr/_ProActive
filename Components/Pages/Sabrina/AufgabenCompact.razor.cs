using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;
using System.Security.Claims;

namespace ProActive2508.Components.Pages.Sabrina;

public partial class AufgabenCompact : ComponentBase
{
    [Parameter] public int MaxItems { get; set; } = 5;

    // Inject properties are provided by the .razor file's @inject directives,
    // remove duplicate [Inject] declarations here to avoid ambiguity.

    protected bool isLoading = true;
    protected bool isModalOpen = false;
    protected string modalTitle = string.Empty;

    protected List<Aufgabe> tasks = new();
    protected Aufgabe editModel = new Aufgabe();

    protected List<Projekt> projektChoicesForModal = new();
    protected string projektSearch = string.Empty;

    protected List<Benutzer> benutzerChoicesForModal = new();
    protected string bearbeiterSearch = string.Empty;

    protected int CurrentUserId { get; private set; }
    protected string CurrentUserEmail { get; private set; } = string.Empty;
    protected bool isProjektleiter = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            AuthenticationState auth = await Auth.GetAuthenticationStateAsync();
            ClaimsPrincipal user = auth.User;

            CurrentUserEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity?.Name ?? string.Empty;

            string? idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            int parsed = 0;
            if (!int.TryParse(idClaim, out parsed) || parsed <= 0)
            {
                parsed = await Db.Set<Benutzer>().Where(b => b.Email == CurrentUserEmail).Select(b => b.Id).FirstOrDefaultAsync();
            }
            CurrentUserId = parsed;
            isProjektleiter = user.IsInRole("Projektleiter");

            // lade kompakte Aufgabenliste für aktuellen Benutzer (ähnlich wie Aufgabenseite)
            List<Aufgabe> loaded = await Db.Set<Aufgabe>()
                .AsNoTracking()
                .Where(a => a.BenutzerId == CurrentUserId)
                .OrderBy(a => a.Faellig)
                .ToListAsync();

            tasks = loaded;

            // Modal Hilfsdaten
            Projekt keinProjekt = new Projekt { Id = 0, Projektbeschreibung = "(kein Projekt)" };
            List<Projekt> relevante = await Db.Set<Projekt>()
                .Where(p => p.BenutzerId == CurrentUserId || p.ProjektleiterId == CurrentUserId)
                .OrderBy(p => p.Projektbeschreibung)
                .ToListAsync();

            projektChoicesForModal = new List<Projekt> { keinProjekt };
            projektChoicesForModal.AddRange(relevante);

            SetBearbeiterToCurrentUser();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    protected bool CanEdit(Aufgabe a) => isProjektleiter || a.BenutzerId == CurrentUserId;

    protected void OpenCreateModal()
    {
        editModel = new Aufgabe
        {
            BenutzerId = CurrentUserId,
            ErstellVon = CurrentUserId,
            Faellig = DateTime.Today.AddDays(7),
            Erledigt = Erledigungsstatus.Offen,
            ProjektId = null
        };
        modalTitle = "Neue Aufgabe";
        SetBearbeiterToCurrentUser();
        isModalOpen = true;
    }

    protected async Task OpenEditModal(int id)
    {
        Aufgabe? found = await Db.Set<Aufgabe>().AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (found == null) return;

        editModel = new Aufgabe
        {
            Id = found.Id,
            ProjektId = found.ProjektId,
            BenutzerId = found.BenutzerId,
            Aufgabenbeschreibung = found.Aufgabenbeschreibung,
            Faellig = found.Faellig,
            Phase = found.Phase,
            Erledigt = found.Erledigt,
            ErstellVon = found.ErstellVon
        };

        modalTitle = $"Aufgabe #{id} bearbeiten";
        await RefreshBearbeiterChoicesAsync(editModel.ProjektId);
        isModalOpen = true;
    }

    protected void CloseModal()
    {
        isModalOpen = false;
    }

    protected async Task SaveAsync()
    {
        if (editModel.ErstellVon <= 0) editModel.ErstellVon = CurrentUserId;
        if (!isProjektleiter) editModel.BenutzerId = CurrentUserId;

        try
        {
            if (editModel.Id == 0) Db.Set<Aufgabe>().Add(editModel);
            else Db.Set<Aufgabe>().Update(editModel);

            await Db.SaveChangesAsync();
            isModalOpen = false;
            await LoadAsync();
        }
        catch
        {
            // für Compact-View einfache Fehlerunterdrückung; detailierte UI-Fehleranzeige bleibt auf kompletter Aufgabenseite
        }
    }

    protected async Task OnProjektPicked(Projekt? p)
    {
        editModel.ProjektId = p == null || p.Id <= 0 ? null : p.Id;
        await RefreshBearbeiterChoicesAsync(editModel.ProjektId);
    }

    protected void OnBearbeiterSelected(Benutzer? b)
    {
        if (!isProjektleiter || b == null) return;
        editModel.BenutzerId = b.Id;
    }

    private void SetBearbeiterToCurrentUser()
    {
        editModel.BenutzerId = CurrentUserId;
        benutzerChoicesForModal = new List<Benutzer>
        {
            new Benutzer { Id = CurrentUserId, Email = CurrentUserEmail }
        };
        bearbeiterSearch = CurrentUserEmail;
    }

    private async Task RefreshBearbeiterChoicesAsync(int? projektId)
    {
        if (!isProjektleiter || !projektId.HasValue || projektId.Value <= 0)
        {
            SetBearbeiterToCurrentUser();
            return;
        }

        var projekt = await Db.Set<Projekt>().AsNoTracking().Where(p => p.Id == projektId.Value)
            .Select(p => new { p.Id, p.ProjektleiterId }).FirstOrDefaultAsync();

        if (projekt == null || projekt.ProjektleiterId != CurrentUserId)
        {
            SetBearbeiterToCurrentUser();
            return;
        }

        List<int> benutzerIds = await Db.Set<ProjektBenutzer>()
            .AsNoTracking()
            .Where(pb => pb.ProjektId == projektId.Value)
            .Select(pb => pb.BenutzerId)
            .Distinct()
            .ToListAsync();

        if (!benutzerIds.Contains(CurrentUserId)) benutzerIds.Add(CurrentUserId);

        List<Benutzer> benutzer = await Db.Set<Benutzer>()
            .AsNoTracking()
            .Where(b => benutzerIds.Contains(b.Id))
            .Select(b => new Benutzer { Id = b.Id, Email = b.Email ?? string.Empty })
            .OrderBy(b => b.Email)
            .ToListAsync();

        benutzerChoicesForModal = benutzer;
        editModel.BenutzerId = 0;
        bearbeiterSearch = string.Empty;
    }

    protected string GetStatusClass(Erledigungsstatus s)
    {
        return s switch
        {
            Erledigungsstatus.Offen => "status-offen",
            Erledigungsstatus.InBearbeitung => "status-inbearbeitung",
            Erledigungsstatus.Erledigt => "status-erledigt",
            _ => "status-offen"
        };
    }
}