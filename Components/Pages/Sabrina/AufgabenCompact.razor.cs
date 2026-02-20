using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja;
using System.Security.Claims;

namespace ProActive2508.Components.Pages.Sabrina;

// Kompaktes Widget, das eine kurze Aufgabenliste für den aktuellen Benutzer anzeigt.
// Verantwortlichkeiten:
// - Laden einer kompakten Aufgabenliste für den aktuellen Benutzer
// - Bereitstellung einfacher Create/Edit-Modalfunktionen
// - Hilfsmethoden zum Auswählen von Projekt / Bearbeiter
public partial class AufgabenCompact : ComponentBase
{
    [Parameter] public int MaxItems { get; set; } = 5;

    // Inject-Eigenschaften werden in der zugehörigen .razor-Datei per @inject bereitgestellt.
    // Hier keine zusätzlichen [Inject]-Deklarationen, um Mehrdeutigkeiten zu vermeiden.

    protected bool isLoading = true;
    protected bool isModalOpen = false;
    protected string modalTitle = string.Empty;

    // Angezeigte Aufgaben (kompakte Liste)
    protected List<Aufgabe> tasks = new();
    // Modell, das im Create/Edit-Modal verwendet wird
    protected Aufgabe editModel = new Aufgabe();

    // Hilfslisten für Modal-Dropdowns
    protected List<Projekt> projektChoicesForModal = new();
    protected string projektSearch = string.Empty;

    protected List<Benutzer> benutzerChoicesForModal = new();
    protected string bearbeiterSearch = string.Empty;

    // Kontext des aktuellen Benutzers (wird in LoadAsync gefüllt)
    protected int CurrentUserId { get; private set; }
    protected string CurrentUserEmail { get; private set; } = string.Empty;
    protected bool isProjektleiter = false;

    // Komponenten-Lifecycle: beim Initialisieren Daten laden
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    // LoadAsync: Lädt die kompakte Aufgabenliste und die unterstützenden Modal-Daten.
    // - Liest den AuthenticationState, um den aktuellen Benutzer zu ermitteln
    // - Fragt Aufgaben und Projekt-Auswahlmöglichkeiten aus der DB ab
    // Das ist die zentrale Datenlade-Methode der Komponente
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
                // Fallback: Lookup per E-Mail in der Datenbank
                parsed = await Db.Set<Benutzer>().Where(b => b.Email == CurrentUserEmail).Select(b => b.Id).FirstOrDefaultAsync();
            }
            CurrentUserId = parsed;
            isProjektleiter = user.IsInRole("Projektleiter");

            // Kompakte Aufgabenliste für den aktuellen Benutzer laden
            List<Aufgabe> loaded = await Db.Set<Aufgabe>()
                .AsNoTracking()
                .Where(a => a.BenutzerId == CurrentUserId)
                .OrderBy(a => a.Faellig)
                .ToListAsync();

            tasks = loaded;

            // Modal-Hilfsdaten: Projekte, bei denen der Benutzer Owner oder Projektleiter ist
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

    // CanEdit: einfache Berechtigungsprüfung für die UI
    protected bool CanEdit(Aufgabe a) => isProjektleiter || a.BenutzerId == CurrentUserId;

    // OpenCreateModal: Bereitet das Modal-Modell für das Anlegen einer neuen Aufgabe vor
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

    // OpenEditModal: Lädt eine einzelne Aufgabe und öffnet das Modal zum Bearbeiten
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

    // CloseModal: Schließt das Modal ohne zu speichern
    protected void CloseModal()
    {
        isModalOpen = false;
    }

    // SaveAsync: Speichert das editModel in die Datenbank (Anlage oder Update)
    // Hinweis: In der Compact-Ansicht werden Fehler bewusst nicht detailliert angezeigt
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
            // In der Compact-Ansicht: einfache Fehlerunterdrückung
        }
    }

    // OnProjektPicked: Wird aufgerufen, wenn im Modal ein Projekt gewählt wurde
    protected async Task OnProjektPicked(Projekt? p)
    {
        editModel.ProjektId = p == null || p.Id <= 0 ? null : p.Id;
        await RefreshBearbeiterChoicesAsync(editModel.ProjektId);
    }

    // OnBearbeiterSelected: Setzt den Bearbeiter, falls aktueller Benutzer Projektleiter ist
    protected void OnBearbeiterSelected(Benutzer? b)
    {
        if (!isProjektleiter || b == null) return;
        editModel.BenutzerId = b.Id;
    }

    // SetBearbeiterToCurrentUser: Hilfsfunktion, um den Bearbeiter standardmäßig auf den aktuellen Nutzer zu setzen
    private void SetBearbeiterToCurrentUser()
    {
        editModel.BenutzerId = CurrentUserId;
        benutzerChoicesForModal = new List<Benutzer>
        {
            new Benutzer { Id = CurrentUserId, Email = CurrentUserEmail }
        };
        bearbeiterSearch = CurrentUserEmail;
    }

    // RefreshBearbeiterChoicesAsync: Lädt mögliche Bearbeiter für ein Projekt.
    // Falls der aktuelle Benutzer nicht Projektleiter ist, wird auf den aktuellen Benutzer reduziert.
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

    // UI-Helfer: mappt Status auf CSS-Klasse
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