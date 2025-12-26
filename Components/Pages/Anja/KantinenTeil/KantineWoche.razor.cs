// Datei: Components/Pages/Anja/KantinenTeil/KantineWoche.razor.cs
// Seite: KantineWoche (Code-Behind)

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using ProActive2508.Data;
using ProActive2508.Models.Entity.Anja.Kantine;
using ProActive2508.Service;
using System.Globalization;
using System.Security.Claims;

namespace ProActive2508.Components.Pages.Anja.KantinenTeil
{
    public partial class KantineWoche : ComponentBase, IDisposable
    {
        [Parameter] public int? numerischeRoutenZahl { get; set; }

        [Inject] public IKantineWeekService WeekService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public AuthenticationStateProvider Auth { get; set; } = default!;
        [Inject] public AppDbContext Db { get; set; } = default!; 
        [Inject] public IDbContextFactory<AppDbContext> DbFactory { get; set; } = default!; // für parallele Kurz-Events
        [Inject] public IJSRuntime JS { get; set; } = default!;
        [Inject] public IMenuplanPdfService PdfService { get; set; } = default!;

        protected bool Loading { get; set; }
        protected bool HasPlan { get; set; }
        protected bool CanEdit { get; set; }
        protected bool IsEditMode { get; set; }
        protected string Title { get; set; } = "Diese Woche";

        protected DateTime Monday { get; set; }
        protected DateTime Friday { get; set; }

        protected List<FormRow> WochenFormular { get; set; } = new();
        protected List<MenuePlan> Days { get; set; } = new();

        protected string? Message { get; set; }
        private int welchewoche = 0;

        // Rollen/Benutzer & Vorbestellungs-Status
        protected bool IsKoch { get; set; }
        protected bool IsMitarbeiter { get; set; }
        protected int CurrentUserId { get; set; }
        protected Dictionary<int, int> Counts { get; set; } = new();
        protected HashSet<int> MeineVormerkungen { get; set; } = new();

        private bool _commitBusy;

        protected override async Task OnParametersSetAsync()
        {
            welchewoche = ResolveWelcheWocheFromRoute(numerischeRoutenZahl, Nav.Uri);

            Title = (welchewoche == 0 ? "Diese Woche"
                   : (welchewoche == 1 ? "Nächste Woche"
                   : (welchewoche == 2 ? "In 2 Wochen" : $"In {welchewoche} Wochen")));

            AuthenticationState authState = await Auth.GetAuthenticationStateAsync();
            ClaimsPrincipal user = authState.User;

            HashSet<string> roles = user
                .FindAll(ClaimTypes.Role)
                .Select((Claim r) => r.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            CanEdit = roles.Contains("Koch") || roles.Contains("Admin");

            IsKoch = roles.Contains("Koch");
            IsMitarbeiter = !IsKoch && user.Identity?.IsAuthenticated == true;
            CurrentUserId = GetUserIdFromClaims(user);

            await LoadAsync();
        }
        private int ResolveWelcheWocheFromRoute(int? zahlenendung, string uri)
        {
            if (zahlenendung.HasValue) return zahlenendung.Value;

            string path = new Uri(uri).AbsolutePath.Trim('/').ToLowerInvariant();

            if (path.EndsWith("diesewoche")) return 0;
            if (path.EndsWith("naechstewoche")) return 1;
            if (path.EndsWith("in2wochen")) return 2;

            string[] seg = path.Split('/', StringSplitOptions.RemoveEmptyEntries);


            if (seg.Length >= 2 && seg[^2] == "kantine")
            {
                int parsed;
                if (int.TryParse(seg[^1], out parsed)) return parsed;
            }

            return 0;
        }
        private async Task LoadAsync()
        {
            Loading = true;

            ValueTuple<DateTime, DateTime> range = WeekService.GetWeekRange(DateTime.Today, welchewoche);
            Monday = range.Item1;
            Friday = range.Item2;

            HasPlan = await WeekService.WeekHasPlanAsync(welchewoche);

            if (HasPlan && !IsEditMode)
            {
                List<MenueplanTag> data = await WeekService.LoadWeekAsync(welchewoche);

                Days = data.Select((MenueplanTag t) => new MenuePlan
                {
                    Tag = t.Tag,
                    Menu1 = t.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 1)?.Gericht.Gerichtname ?? "—",
                    Menu2 = t.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 2)?.Gericht.Gerichtname ?? "—",
                    EintragId1 = t.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 1)?.Id ?? 0,
                    EintragId2 = t.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 2)?.Id ?? 0,
                    MenueplanTagId = t.Id
                }).ToList();

                List<int> eintragIds = Days
                    .SelectMany((MenuePlan d) => new[] { d.EintragId1, d.EintragId2 })
                    .Where((int id) => id > 0)
                    .ToList();

                Counts = await Db.Vorbestellungen
                    .Where((Vorbestellung v) => eintragIds.Contains(v.EintragId))
                    .GroupBy((Vorbestellung v) => v.EintragId)
                    .Select((IGrouping<int, Vorbestellung> g) => new { Key = g.Key, Cnt = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Cnt);

                MeineVormerkungen = await Db.Vorbestellungen
                    .Where(v => v.BenutzerId == CurrentUserId && eintragIds.Contains(v.EintragId))
                    .Select(v => v.EintragId)
                    .ToHashSetAsync();//anders als liste erlaubt HashSet keine duplikate keine doppelte auswahl von Menüs erlaubt

                WochenFormular.Clear();

                
            }
            else
            {
                await FillFormForWeekAsync();
            }

            Loading = false;
        }

        protected override void OnInitialized()
        {
            Nav.LocationChanged += HandleLocationChanged;
        }

        private async void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            IsEditMode = false;
            welchewoche = ResolveWelcheWocheFromRoute(numerischeRoutenZahl, e.Location);

            Title = (welchewoche == 0 ? "Diese Woche"
                   : (welchewoche == 1 ? "Nächste Woche"
                   : (welchewoche == 2 ? "In 2 Wochen" : $"In {welchewoche} Wochen")));

            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        }

        

       
        
        

        protected int GetCount(int eintragId)
        {
            int c;
            return Counts.TryGetValue(eintragId, out c) ? c : 0;
        }

        protected async Task ToggleReservationExclusive(int menueplanTagId, int eintragId)
        {
            if (CurrentUserId <= 0 || eintragId <= 0) return;

            Vorbestellung? existing = await Db.Vorbestellungen
                .FirstOrDefaultAsync((Vorbestellung x) => x.BenutzerId == CurrentUserId && x.MenueplanTagId == menueplanTagId);

            if (existing is null)
            {
                Db.Vorbestellungen.Add(new Vorbestellung
                {
                    BenutzerId = CurrentUserId,
                    MenueplanTagId = menueplanTagId,
                    EintragId = eintragId
                });

                await Db.SaveChangesAsync();

                MeineVormerkungen.Add(eintragId);
                Counts[eintragId] = GetCount(eintragId) + 1;
            }
            else if (existing.EintragId != eintragId)
            {
                int oldId = existing.EintragId;
                existing.EintragId = eintragId;
                Db.Vorbestellungen.Update(existing);
                await Db.SaveChangesAsync();

                MeineVormerkungen.Remove(oldId);
                MeineVormerkungen.Add(eintragId);

                Counts[oldId] = Math.Max(0, GetCount(oldId) - 1);
                Counts[eintragId] = GetCount(eintragId) + 1;
            }

            StateHasChanged();
        }

        protected async Task RemoveReservationAsync(int menueplanTagId)
        {
            if (CurrentUserId <= 0) return;

            Vorbestellung? existing = await Db.Vorbestellungen
                .FirstOrDefaultAsync((Vorbestellung x) => x.BenutzerId == CurrentUserId && x.MenueplanTagId == menueplanTagId);

            if (existing is null) return;

            int oldEintragId = existing.EintragId;

            Db.Vorbestellungen.Remove(existing);
            await Db.SaveChangesAsync();

            MeineVormerkungen.Remove(oldEintragId);
            if (Counts.ContainsKey(oldEintragId))
                Counts[oldEintragId] = Math.Max(0, Counts[oldEintragId] - 1);

            StateHasChanged();
        }

        protected async Task RevokePublicationAsync()
        {
            if (!CanEdit) return;

            bool ok = await JS.InvokeAsync<bool>("confirm",
                $"Veröffentlichung für „{Title}“ zurückziehen?\nAlle Einträge & Vormerkungen dieser Woche werden gelöscht.");
            if (!ok) return;

            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await Db.Database.BeginTransactionAsync();

            List<MenueplanTag> tags = await Db.Set<MenueplanTag>()
                .Where((MenueplanTag t) => t.Tag >= Monday.Date && t.Tag <= Friday.Date)
                .Include((MenueplanTag t) => t.Eintraege)
                .ToListAsync();

            List<int> entryIds = tags.SelectMany((MenueplanTag t) => t.Eintraege).Select((Menueplan e) => e.Id).ToList();

            if (entryIds.Count > 0)
            {
                List<Vorbestellung> vorm = await Db.Vorbestellungen.Where((Vorbestellung v) => entryIds.Contains(v.EintragId)).ToListAsync();
                if (vorm.Count > 0) Db.Vorbestellungen.RemoveRange(vorm);

                Db.Set<Menueplan>().RemoveRange(tags.SelectMany((MenueplanTag t) => t.Eintraege));
            }

            if (tags.Count > 0)
                Db.Set<MenueplanTag>().RemoveRange(tags);

            await Db.SaveChangesAsync();
            await tx.CommitAsync();

            HasPlan = false;
            Days.Clear();
            Counts.Clear();
            MeineVormerkungen.Clear();
            Message = "Veröffentlichung wurde zurückgezogen.";

            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task FillFormForWeekAsync()
        {
            WochenFormular = Enumerable.Range(0, 5)
                                       .Select((int i) => new FormRow { Tag = Monday.AddDays(i) })
                                       .ToList();

            if (HasPlan)
            {
                List<MenueplanTag> data = await WeekService.LoadWeekAsync(welchewoche);
                foreach (FormRow row in WochenFormular)
                {
                    MenueplanTag? day = data.FirstOrDefault((MenueplanTag d) => d.Tag.Date == row.Tag.Date);
                    if (day is null) continue;

                    string? e1 = day.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 1)?.Gericht?.Gerichtname;
                    string? e2 = day.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 2)?.Gericht?.Gerichtname;

                    row.Menu1 = e1;
                    row.Menu2 = e2;

                    if (!string.IsNullOrWhiteSpace(e1))
                    {
                        GerichtInfo? info = await WeekService.FindGerichtInfoAsync(e1);
                        if (info != null) { row.Menu1Allergene = info.Allergene; row.Menu1Preis = info.LastPrice; }
                    }
                    if (!string.IsNullOrWhiteSpace(e2))
                    {
                        GerichtInfo? info = await WeekService.FindGerichtInfoAsync(e2);
                        if (info != null) { row.Menu2Allergene = info.Allergene; row.Menu2Preis = info.LastPrice; }
                    }
                }
            }

            for (int i = 0; i < WochenFormular.Count; i++)
            {
                FormRow r = WochenFormular[i];
                r.Menu1Search = r.Menu1 ?? string.Empty;
                r.Menu2Search = r.Menu2 ?? string.Empty;
                r.Menu1Items = await QueryGerichteAsync(r.Menu1Search);
                r.Menu2Items = await QueryGerichteAsync(r.Menu2Search);
            }
        }

        protected async Task EnterEditModeAsync()
        {
            if (!CanEdit) return;
            IsEditMode = true;
            await FillFormForWeekAsync();
            await InvokeAsync(StateHasChanged);
        }

        protected async Task CancelEditAsync()
        {
            IsEditMode = false;
            WochenFormular.Clear();
            await LoadAsync();
        }

        protected bool IsFormComplete =>
            WochenFormular.Count > 0 &&
            WochenFormular.All((FormRow r) =>
                !string.IsNullOrWhiteSpace(r.Menu1) &&
                !string.IsNullOrWhiteSpace(r.Menu2) &&
                !string.IsNullOrWhiteSpace(r.Menu1Allergene) &&
                !string.IsNullOrWhiteSpace(r.Menu2Allergene) &&
                r.Menu1Preis.HasValue && r.Menu1Preis.Value > 0m &&
                r.Menu2Preis.HasValue && r.Menu2Preis.Value > 0m
            );

        private (bool ok, string msg) ValidateWeekForm()
        {
            if (WochenFormular.Count == 0)
                return (false, "Keine Tage im Formular.");

            CultureInfo ci = new CultureInfo("de-DE");

            for (int i = 0; i < WochenFormular.Count; i++)
            {
                FormRow r = WochenFormular[i];
                string tagText = r.Tag.ToString("dddd, dd.MM.yyyy", ci);

                if (string.IsNullOrWhiteSpace(r.Menu1) || string.IsNullOrWhiteSpace(r.Menu2))
                    return (false, $"Bitte am {tagText} beide Menüs eintragen.");

                if (string.IsNullOrWhiteSpace(r.Menu1Allergene) || string.IsNullOrWhiteSpace(r.Menu2Allergene))
                    return (false, $"Bitte am {tagText} Allergene für beide Menüs angeben.");

                if (!r.Menu1Preis.HasValue || r.Menu1Preis.Value <= 0m ||
                    !r.Menu2Preis.HasValue || r.Menu2Preis.Value <= 0m)
                    return (false, $"Bitte am {tagText} für beide Menüs einen Preis > 0 eintragen.");
            }

            return (true, string.Empty);
        }

        protected async Task OnAllergeneChanged(int rowIndex, int which)
        {
            if (rowIndex < 0 || rowIndex >= WochenFormular.Count) return;
            FormRow row = WochenFormular[rowIndex];

            string? gerichtName = (which == 1 ? row.Menu1 : row.Menu2)?.Trim();
            string codes = (which == 1 ? row.Menu1Allergene : row.Menu2Allergene) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(gerichtName)) return;

            await EnsureGerichtExistsAsync(gerichtName);
            await WeekService.UpdateAllergeneForGerichtAsync(gerichtName, codes);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnPriceChanged(int rowIndex, int which)
        {
            if (rowIndex < 0 || rowIndex >= WochenFormular.Count) return;

            FormRow row = WochenFormular[rowIndex];
            string? gerichtName = (which == 1 ? row.Menu1 : row.Menu2)?.Trim();
            decimal? neuerPreis = (which == 1 ? row.Menu1Preis : row.Menu2Preis);

            if (string.IsNullOrWhiteSpace(gerichtName) || !neuerPreis.HasValue) return;

            decimal rounded = Math.Round(neuerPreis.Value, 2, MidpointRounding.AwayFromZero);

            await EnsureGerichtExistsAsync(gerichtName);
            await WeekService.EnsurePreisForGerichtAsync(gerichtName, rounded);

            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnMenuSearchChangedAsync(int rowIndex, int which, string value)
        {
            FormRow row = WochenFormular[rowIndex];

            if (which == 1)
            {
                row.Menu1Search = value;
                List<string> list = await QueryGerichteAsync(value);
                if (!string.IsNullOrWhiteSpace(value) && !list.Any((string s) => s.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    list.Insert(0, value.Trim());
                row.Menu1Items = list;
            }
            else
            {
                row.Menu2Search = value;
                List<string> list = await QueryGerichteAsync(value);
                if (!string.IsNullOrWhiteSpace(value) && !list.Any((string s) => s.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    list.Insert(0, value.Trim());
                row.Menu2Items = list;
            }

            StateHasChanged();
        }

        protected async Task OnMenuSelected(int rowIndex, int which, string value)
        {
            FormRow row = WochenFormular[rowIndex];

            if (which == 1)
            {
                row.Menu1 = value;
                row.Menu1Search = value;
            }
            else
            {
                row.Menu2 = value;
                row.Menu2Search = value;
            }

            await OnMenuChanged(rowIndex, which, forcePopulate: true);
        }

        protected async Task OnMenuTextCommittedAsync(int rowIndex, int which, string value)
        {
            if (_commitBusy) return;
            _commitBusy = true;

            try
            {
                string name = (value ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name) || rowIndex < 0 || rowIndex >= WochenFormular.Count) return;

                int _ = await EnsureGerichtExistsAsync(name);

                FormRow row = WochenFormular[rowIndex];

                if (which == 1) { row.Menu1 = name; row.Menu1Search = name; }
                else { row.Menu2 = name; row.Menu2Search = name; }

                List<string> fresh = await QueryGerichteAsync(name);
                if (!fresh.Any((string s) => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    fresh.Insert(0, name);

                if (which == 1) row.Menu1Items = fresh;
                else row.Menu2Items = fresh;

                await OnMenuChanged(rowIndex, which, forcePopulate: true);

                await InvokeAsync(StateHasChanged);
            }
            finally { _commitBusy = false; }
        }

        private async Task RefreshAllSearchListsAsync(string? ensureName = null)
        {
            for (int i = 0; i < WochenFormular.Count; i++)
            {
                FormRow r = WochenFormular[i];

                List<string> list1 = await QueryGerichteAsync(r.Menu1Search);
                if (!string.IsNullOrWhiteSpace(ensureName) &&
                    !list1.Any((string s) => s.Equals(ensureName, StringComparison.OrdinalIgnoreCase)))
                {
                    list1.Insert(0, ensureName);
                }
                r.Menu1Items = list1;

                List<string> list2 = await QueryGerichteAsync(r.Menu2Search);
                if (!string.IsNullOrWhiteSpace(ensureName) &&
                    !list2.Any((string s) => s.Equals(ensureName, StringComparison.OrdinalIgnoreCase)))
                {
                    list2.Insert(0, ensureName);
                }
                r.Menu2Items = list2;
            }
        }

        private async Task<List<string>> QueryGerichteAsync(string? term)
        {
            await using AppDbContext ctx = await DbFactory.CreateDbContextAsync();

            IQueryable<string> q = ctx.Gerichte.AsNoTracking().Select((Gericht g) => g.Gerichtname);
            if (!string.IsNullOrWhiteSpace(term))
            {
                string t = term.Trim();
                q = q.Where((string n) => EF.Functions.Like(n, $"%{t}%"));
            }

            return await q.OrderBy((string n) => n).Take(20).ToListAsync();
        }

        protected async Task OnMenuChanged(int index, int which, bool forcePopulate = false)
        {
            FormRow row = WochenFormular[index];
            string name = (which == 1 ? row.Menu1 : row.Menu2) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return;

            GerichtInfo? info = await WeekService.FindGerichtInfoAsync(name);
            if (info is null) return;

            if (which == 1)
            {
                if (forcePopulate || string.IsNullOrWhiteSpace(row.Menu1Allergene))
                    row.Menu1Allergene = info.Allergene ?? string.Empty;

                if (forcePopulate || !row.Menu1Preis.HasValue || row.Menu1Preis.Value <= 0m)
                    row.Menu1Preis = info.LastPrice;
            }
            else
            {
                if (forcePopulate || string.IsNullOrWhiteSpace(row.Menu2Allergene))
                    row.Menu2Allergene = info.Allergene ?? string.Empty;

                if (forcePopulate || !row.Menu2Preis.HasValue || row.Menu2Preis.Value <= 0m)
                    row.Menu2Preis = info.LastPrice;
            }

            StateHasChanged();
        }

        protected async Task SaveAsync()
        {
            (bool ok, string msg) check = ValidateWeekForm();
            if (!check.ok)
            {
                Message = check.msg;
                return;
            }

            try
            {
                await ReplaceWeekPreservingReservationsAsync();
                await PersistStammdatenAsync();

                Message = IsEditMode ? $"{Title} wurde aktualisiert." : $"{Title} wurde angelegt.";
                HasPlan = true;
                IsEditMode = false;

                await LoadAsync();
            }
            catch (Exception ex)
            {
                Message = "Fehler beim Speichern: " + ex.Message;
            }
        }

        private async Task PersistStammdatenAsync()
        {
            foreach (FormRow r in WochenFormular)
            {
                if (!string.IsNullOrWhiteSpace(r.Menu1))
                {
                    string n1 = r.Menu1.Trim();
                    await EnsureGerichtExistsAsync(n1);
                    await WeekService.UpdateAllergeneForGerichtAsync(n1, r.Menu1Allergene ?? string.Empty);

                    if (r.Menu1Preis.HasValue)
                    {
                        decimal p1 = Math.Round(r.Menu1Preis.Value, 2, MidpointRounding.AwayFromZero);
                        await WeekService.EnsurePreisForGerichtAsync(n1, p1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(r.Menu2))
                {
                    string n2 = r.Menu2.Trim();
                    await EnsureGerichtExistsAsync(n2);
                    await WeekService.UpdateAllergeneForGerichtAsync(n2, r.Menu2Allergene ?? string.Empty);

                    if (r.Menu2Preis.HasValue)
                    {
                        decimal p2 = Math.Round(r.Menu2Preis.Value, 2, MidpointRounding.AwayFromZero);
                        await WeekService.EnsurePreisForGerichtAsync(n2, p2);
                    }
                }
            }
        }

        private async Task ReplaceWeekPreservingReservationsAsync()
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await Db.Database.BeginTransactionAsync();

            List<MenueplanTag> existingTags = await Db.Set<MenueplanTag>()
                .Where((MenueplanTag t) => t.Tag >= Monday.Date && t.Tag <= Friday.Date)
                .Include((MenueplanTag t) => t.Eintraege)
                .ToListAsync();

            Dictionary<DateTime, MenueplanTag> byDate = existingTags.ToDictionary((MenueplanTag t) => t.Tag.Date);

            List<(DateTime Tag, int Gericht1, int Gericht2)> desired = new List<(DateTime Tag, int Gericht1, int Gericht2)>();
            foreach (FormRow r in WochenFormular.OrderBy((FormRow x) => x.Tag))
            {
                int g1 = await ResolveGerichtIdAsync(r.Menu1!.Trim());
                int g2 = await ResolveGerichtIdAsync(r.Menu2!.Trim());
                desired.Add((r.Tag.Date, g1, g2));
            }

            foreach ((DateTime Tag, int Gericht1, int Gericht2) d in desired)
            {
                MenueplanTag? tagEntity;
                if (!byDate.TryGetValue(d.Tag, out tagEntity))
                {
                    tagEntity = new MenueplanTag { Tag = d.Tag };
                    Db.Set<MenueplanTag>().Add(tagEntity);

                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTag = tagEntity, PositionNr = 1, GerichtId = d.Gericht1 });
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTag = tagEntity, PositionNr = 2, GerichtId = d.Gericht2 });

                    continue;
                }

                Menueplan? old1 = tagEntity.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 1);
                if (old1 is null)
                {
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 1, GerichtId = d.Gericht1 });
                }
                else if (old1.GerichtId != d.Gericht1)
                {
                    List<Vorbestellung> v1 = await Db.Vorbestellungen.Where((Vorbestellung v) => v.EintragId == old1.Id).ToListAsync();
                    if (v1.Count > 0) Db.Vorbestellungen.RemoveRange(v1);

                    Db.Set<Menueplan>().Remove(old1);
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 1, GerichtId = d.Gericht1 });
                }

                Menueplan? old2 = tagEntity.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 2);
                if (old2 is null)
                {
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 2, GerichtId = d.Gericht2 });
                }
                else if (old2.GerichtId != d.Gericht2)
                {
                    List<Vorbestellung> v2 = await Db.Vorbestellungen.Where((Vorbestellung v) => v.EintragId == old2.Id).ToListAsync();
                    if (v2.Count > 0) Db.Vorbestellungen.RemoveRange(v2);

                    Db.Set<Menueplan>().Remove(old2);
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 2, GerichtId = d.Gericht2 });
                }
            }

            HashSet<DateTime> desiredDates = desired.Select(((DateTime Tag, int Gericht1, int Gericht2) x) => x.Tag).ToHashSet();
            List<MenueplanTag> toDeleteTags = existingTags.Where((MenueplanTag t) => !desiredDates.Contains(t.Tag.Date)).ToList();

            if (toDeleteTags.Count > 0)
            {
                List<int> delEntryIds = toDeleteTags.SelectMany((MenueplanTag t) => t.Eintraege).Select((Menueplan e) => e.Id).ToList();
                if (delEntryIds.Count > 0)
                {
                    List<Vorbestellung> v = await Db.Vorbestellungen.Where((Vorbestellung x) => delEntryIds.Contains(x.EintragId)).ToListAsync();
                    if (v.Count > 0) Db.Vorbestellungen.RemoveRange(v);

                    Db.Set<Menueplan>().RemoveRange(toDeleteTags.SelectMany((MenueplanTag t) => t.Eintraege));
                }

                Db.Set<MenueplanTag>().RemoveRange(toDeleteTags);
            }

            await Db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private async Task<int> ResolveGerichtIdAsync(string name)
        {
            string n = (name ?? string.Empty).Trim();
            Gericht? existing = await Db.Gerichte.FirstOrDefaultAsync((Gericht x) => x.Gerichtname.ToUpper() == n.ToUpper());
            if (existing is not null) return existing.Id;

            Gericht neu = new Gericht { Gerichtname = n };
            Db.Gerichte.Add(neu);

            try
            {
                await Db.SaveChangesAsync();
                return neu.Id;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                Gericht winner = await Db.Gerichte.AsNoTracking()
                    .FirstAsync((Gericht x) => x.Gerichtname.ToUpper() == n.ToUpper());
                return winner.Id;
            }
        }

        private async Task<int> EnsureGerichtExistsAsync(string name)
        {
            string n = (name ?? string.Empty).Trim();
            if (n.Length == 0) return 0;

            await using AppDbContext ctx = await DbFactory.CreateDbContextAsync();

            Gericht? existing = await ctx.Gerichte
                .AsNoTracking()
                .FirstOrDefaultAsync((Gericht x) => x.Gerichtname.ToUpper() == n.ToUpper());
            if (existing is not null) return existing.Id;

            Gericht neu = new Gericht { Gerichtname = n };
            ctx.Gerichte.Add(neu);

            try
            {
                await ctx.SaveChangesAsync();
                return neu.Id;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                Gericht winner = await ctx.Gerichte
                    .AsNoTracking()
                    .FirstAsync((Gericht x) => x.Gerichtname.ToUpper() == n.ToUpper());
                return winner.Id;
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            if (ex.GetBaseException() is SqlException sql)
                return sql.Number == 2601 || sql.Number == 2627; // Unique index/constraint
            return false;
        }

        // ---- PDF-Export ----
        protected async Task ExportPdfAsync()
        {
            List<MenueplanTag> data = await WeekService.LoadWeekAsync(welchewoche);

            List<PdfMenuDay> pdfDays = new List<PdfMenuDay>();
            decimal? commonPrice = null;

            foreach (MenueplanTag t in data.OrderBy((MenueplanTag d) => d.Tag))
            {
                string e1 = t.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 1)?.Gericht?.Gerichtname ?? string.Empty;
                string e2 = t.Eintraege.FirstOrDefault((Menueplan e) => e.PositionNr == 2)?.Gericht?.Gerichtname ?? string.Empty;

                GerichtInfo? i1 = string.IsNullOrWhiteSpace(e1) ? null : await WeekService.FindGerichtInfoAsync(e1);
                GerichtInfo? i2 = string.IsNullOrWhiteSpace(e2) ? null : await WeekService.FindGerichtInfoAsync(e2);

                if (i1?.LastPrice is decimal p1) commonPrice ??= p1;
                if (i2?.LastPrice is decimal p2) commonPrice ??= p2;

                pdfDays.Add(new PdfMenuDay
                {
                    Date = t.Tag.Date,
                    Menu1 = e1,
                    Menu1Allergens = i1?.Allergene ?? string.Empty,
                    Menu2 = e2,
                    Menu2Allergens = i2?.Allergene ?? string.Empty
                });
            }

            string logoPath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "images", "logo.png");
            if (!File.Exists(logoPath)) logoPath = null!;

            byte[] bytes = await PdfService.BuildMenuplanPdfAsync(
                Monday.Date, Friday.Date,
                kantinenName: "MAGNA Weiz",
                title: "MENÜPLAN",
                days: pdfDays,
                mealPrice: commonPrice ?? 7.19m,
                logoPath: logoPath
            );

            string base64 = Convert.ToBase64String(bytes);
            string fileName = $"Menueplan_{Monday:yyyyMMdd}_{Friday:yyyyMMdd}.pdf";
            await JS.InvokeVoidAsync("downloadFileFromBytes", fileName, base64);
        }

        public void Dispose()
        {
            Nav.LocationChanged -= HandleLocationChanged;
        }

        private int GetUserIdFromClaims(ClaimsPrincipal user)
        {
            string idStr =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("sub") ??
                user.FindFirstValue("user_id") ??
                "0";

            int id;
            return int.TryParse(idStr, out id) ? id : 0;
        }

        // ---- VMs ----
        protected class MenuePlan
        {
            public DateTime Tag { get; set; }
            public string Menu1 { get; set; } = string.Empty;
            public string Menu2 { get; set; } = string.Empty;
            public int MenueplanTagId { get; set; }
            public int EintragId1 { get; set; }
            public int EintragId2 { get; set; }
        }

        protected class FormRow
        {
            public DateTime Tag { get; set; }

            public string? Menu1 { get; set; }
            public string? Menu1Allergene { get; set; }
            public decimal? Menu1Preis { get; set; }

            public string Menu1Search { get; set; } = string.Empty;
            public List<string> Menu1Items { get; set; } = new();

            public string? Menu2 { get; set; }
            public string? Menu2Allergene { get; set; }
            public decimal? Menu2Preis { get; set; }

            public string Menu2Search { get; set; } = string.Empty;
            public List<string> Menu2Items { get; set; } = new();
        }
    }
}
