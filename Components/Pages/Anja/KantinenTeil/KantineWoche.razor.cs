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
        [Parameter] public int? AusgewählteWoche { get; set; }

        [Inject] public IKantineWeekService WeekService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public AuthenticationStateProvider Auth { get; set; } = default!;
        [Inject] public AppDbContext Db { get; set; } = default!; // für „große“ Operationen wie Speichern/Zähler
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

        protected override void OnInitialized()
        {
            Nav.LocationChanged += HandleLocationChanged;
        }

        private async void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            IsEditMode = false;
            welchewoche = ResolveWelcheWocheFromRoute(AusgewählteWoche, e.Location);
            Title = (welchewoche == 0 ? "Diese Woche"
                   : (welchewoche == 1 ? "Nächste Woche"
                   : (welchewoche == 2 ? "In 2 Wochen" : $"In {welchewoche} Wochen")));
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        }

        private int ResolveWelcheWocheFromRoute(int? pOffset, string uri)
        {
            if (pOffset.HasValue) return pOffset.Value;
            var path = new Uri(uri).AbsolutePath.Trim('/').ToLowerInvariant();
            if (path.EndsWith("diesewoche")) return 0;
            if (path.EndsWith("naechstewoche")) return 1;
            if (path.EndsWith("in2wochen")) return 2;
            var seg = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (seg.Length >= 2 && seg[^2] == "kantine" && int.TryParse(seg[^1], out var parsed)) return parsed;
            return 0;
        }

        protected override async Task OnParametersSetAsync()
        {
            welchewoche = ResolveWelcheWocheFromRoute(AusgewählteWoche, Nav.Uri);
            Title = (welchewoche == 0 ? "Diese Woche"
                   : (welchewoche == 1 ? "Nächste Woche"
                   : (welchewoche == 2 ? "In 2 Wochen" : $"In {welchewoche} Wochen")));

            var auth = await Auth.GetAuthenticationStateAsync();
            var user = auth.User;
            var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            CanEdit = roles.Contains("Koch") || roles.Contains("Admin");

            IsKoch = roles.Contains("Koch");
            IsMitarbeiter = !IsKoch && user.Identity?.IsAuthenticated == true;
            CurrentUserId = GetUserIdFromClaims(user);

            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Loading = true;

            (Monday, Friday) = WeekService.GetWeekRange(DateTime.Today, welchewoche);
            HasPlan = await WeekService.WeekHasPlanAsync(welchewoche);

            if (HasPlan && !IsEditMode)
            {
                var data = await WeekService.LoadWeekAsync(welchewoche);

                Days = data.Select(t => new MenuePlan
                {
                    Tag = t.Tag,
                    Menu1 = t.Eintraege.FirstOrDefault(e => e.PositionNr == 1)?.Gericht.Gerichtname ?? "—",
                    Menu2 = t.Eintraege.FirstOrDefault(e => e.PositionNr == 2)?.Gericht.Gerichtname ?? "—",
                    EintragId1 = t.Eintraege.FirstOrDefault(e => e.PositionNr == 1)?.Id ?? 0,
                    EintragId2 = t.Eintraege.FirstOrDefault(e => e.PositionNr == 2)?.Id ?? 0,
                    MenueplanTagId = t.Id
                }).ToList();

                var eintragIds = Days.SelectMany(d => new[] { d.EintragId1, d.EintragId2 }).Where(id => id > 0).ToList();

                Counts = await Db.Vorbestellungen
                    .Where(v => eintragIds.Contains(v.EintragId))
                    .GroupBy(v => v.EintragId)
                    .Select(g => new { g.Key, Cnt = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Cnt);

                MeineVormerkungen = (await Db.Vorbestellungen
                        .Where(v => v.BenutzerId == CurrentUserId && eintragIds.Contains(v.EintragId))
                        .Select(v => v.EintragId)
                        .ToListAsync())
                    .ToHashSet();

                WochenFormular.Clear();
            }
            else
            {
                await FillFormForWeekAsync();
            }

            Loading = false;
        }

        protected int GetCount(int eintragId) => Counts.TryGetValue(eintragId, out var c) ? c : 0;

        protected async Task ToggleReservationExclusive(int menueplanTagId, int eintragId)
        {
            if (CurrentUserId <= 0 || eintragId <= 0) return;

            var existing = await Db.Vorbestellungen
                .FirstOrDefaultAsync(x => x.BenutzerId == CurrentUserId && x.MenueplanTagId == menueplanTagId);

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
                var oldId = existing.EintragId;
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

            var existing = await Db.Vorbestellungen
                .FirstOrDefaultAsync(x => x.BenutzerId == CurrentUserId && x.MenueplanTagId == menueplanTagId);

            if (existing is null) return;

            var oldEintragId = existing.EintragId;

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

            var ok = await JS.InvokeAsync<bool>("confirm",
                $"Veröffentlichung für „{Title}“ zurückziehen?\nAlle Einträge & Vormerkungen dieser Woche werden gelöscht.");
            if (!ok) return;

            using var tx = await Db.Database.BeginTransactionAsync();

            var tags = await Db.Set<MenueplanTag>()
                .Where(t => t.Tag >= Monday.Date && t.Tag <= Friday.Date)
                .Include(t => t.Eintraege)
                .ToListAsync();

            var entryIds = tags.SelectMany(t => t.Eintraege).Select(e => e.Id).ToList();

            if (entryIds.Count > 0)
            {
                var vorm = await Db.Vorbestellungen.Where(v => entryIds.Contains(v.EintragId)).ToListAsync();
                if (vorm.Count > 0) Db.Vorbestellungen.RemoveRange(vorm);

                Db.Set<Menueplan>().RemoveRange(tags.SelectMany(t => t.Eintraege));
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
                                       .Select(i => new FormRow { Tag = Monday.AddDays(i) })
                                       .ToList();

            if (HasPlan)
            {
                var data = await WeekService.LoadWeekAsync(welchewoche);
                foreach (var row in WochenFormular)
                {
                    var day = data.FirstOrDefault(d => d.Tag.Date == row.Tag.Date);
                    if (day is null) continue;

                    var e1 = day.Eintraege.FirstOrDefault(e => e.PositionNr == 1)?.Gericht?.Gerichtname;
                    var e2 = day.Eintraege.FirstOrDefault(e => e.PositionNr == 2)?.Gericht?.Gerichtname;

                    row.Menu1 = e1;
                    row.Menu2 = e2;

                    if (!string.IsNullOrWhiteSpace(e1))
                    {
                        var info = await WeekService.FindGerichtInfoAsync(e1);
                        if (info != null) { row.Menu1Allergene = info.Allergene; row.Menu1Preis = info.LastPrice; }
                    }
                    if (!string.IsNullOrWhiteSpace(e2))
                    {
                        var info = await WeekService.FindGerichtInfoAsync(e2);
                        if (info != null) { row.Menu2Allergene = info.Allergene; row.Menu2Preis = info.LastPrice; }
                    }
                }
            }

            for (int i = 0; i < WochenFormular.Count; i++)
            {
                var r = WochenFormular[i];
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
            WochenFormular.All(r =>
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

            var ci = new CultureInfo("de-DE");

            for (int i = 0; i < WochenFormular.Count; i++)
            {
                var r = WochenFormular[i];
                var tagText = r.Tag.ToString("dddd, dd.MM.yyyy", ci);

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
            var row = WochenFormular[rowIndex];

            var gerichtName = (which == 1 ? row.Menu1 : row.Menu2)?.Trim();
            var codes = (which == 1 ? row.Menu1Allergene : row.Menu2Allergene) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(gerichtName)) return;

            // Sicherstellen, dass das Gericht existiert (sonst wären Updates no-op)
            await EnsureGerichtExistsAsync(gerichtName);
            await WeekService.UpdateAllergeneForGerichtAsync(gerichtName, codes);
            await InvokeAsync(StateHasChanged);
        }


        protected async Task OnPriceChanged(int rowIndex, int which)
        {
            if (rowIndex < 0 || rowIndex >= WochenFormular.Count) return;

            var row = WochenFormular[rowIndex];
            var gerichtName = (which == 1 ? row.Menu1 : row.Menu2)?.Trim();
            var neuerPreis = (which == 1 ? row.Menu1Preis : row.Menu2Preis);

            if (string.IsNullOrWhiteSpace(gerichtName) || !neuerPreis.HasValue) return;

            var rounded = Math.Round(neuerPreis.Value, 2, MidpointRounding.AwayFromZero);

            // Sicherstellen, dass das Gericht existiert (sonst wären Updates no-op)
            await EnsureGerichtExistsAsync(gerichtName);
            await WeekService.EnsurePreisForGerichtAsync(gerichtName, rounded);

            await InvokeAsync(StateHasChanged);
        }


        protected async Task OnMenuSearchChangedAsync(int rowIndex, int which, string value)
        {
            var row = WochenFormular[rowIndex];

            if (which == 1)
            {
                row.Menu1Search = value;
                var list = await QueryGerichteAsync(value);
                if (!string.IsNullOrWhiteSpace(value) && !list.Any(s => s.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    list.Insert(0, value.Trim());
                row.Menu1Items = list;
            }
            else
            {
                row.Menu2Search = value;
                var list = await QueryGerichteAsync(value);
                if (!string.IsNullOrWhiteSpace(value) && !list.Any(s => s.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    list.Insert(0, value.Trim());
                row.Menu2Items = list;
            }

            StateHasChanged();
        }

        protected async Task OnMenuSelected(int rowIndex, int which, string value)
        {
            var row = WochenFormular[rowIndex];

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

        // freier Text (Enter/Blur) -> Gericht anlegen/sicherstellen + Stammdaten nachladen
        protected async Task OnMenuTextCommittedAsync(int rowIndex, int which, string value)
        {
            if (_commitBusy) return;
            _commitBusy = true;
            try
            {
                var name = (value ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name) || rowIndex < 0 || rowIndex >= WochenFormular.Count) return;

                // 1) Gericht in der DB sicherstellen (Persist)
                _ = await EnsureGerichtExistsAsync(name);

                var row = WochenFormular[rowIndex];

                // 2) UI-State setzen
                if (which == 1) { row.Menu1 = name; row.Menu1Search = name; }
                else { row.Menu2 = name; row.Menu2Search = name; }

                // 3) Dropdown-Items sofort frisch laden (inkl. neuem Gericht)
                var fresh = await QueryGerichteAsync(name);
                if (!fresh.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    fresh.Insert(0, name);

                if (which == 1) row.Menu1Items = fresh;
                else row.Menu2Items = fresh;

                // 4) Stammdaten (Preis/Allergene) nachziehen → jetzt mit forcePopulate
                await OnMenuChanged(rowIndex, which, forcePopulate: true);

                await InvokeAsync(StateHasChanged);
            }
            finally { _commitBusy = false; }
        }
        private async Task RefreshAllSearchListsAsync(string? ensureName = null)
        {
            for (int i = 0; i < WochenFormular.Count; i++)
            {
                var r = WochenFormular[i];

                var list1 = await QueryGerichteAsync(r.Menu1Search);
                if (!string.IsNullOrWhiteSpace(ensureName) &&
                    !list1.Any(s => s.Equals(ensureName, StringComparison.OrdinalIgnoreCase)))
                {
                    list1.Insert(0, ensureName);
                }
                r.Menu1Items = list1;

                var list2 = await QueryGerichteAsync(r.Menu2Search);
                if (!string.IsNullOrWhiteSpace(ensureName) &&
                    !list2.Any(s => s.Equals(ensureName, StringComparison.OrdinalIgnoreCase)))
                {
                    list2.Insert(0, ensureName);
                }
                r.Menu2Items = list2;
            }
        }

        // nutzt eigenen DbContext aus Factory (keine Parallelnutzung von "Db")
        private async Task<List<string>> QueryGerichteAsync(string? term)
        {
            await using var ctx = await DbFactory.CreateDbContextAsync();
            var q = ctx.Gerichte.AsNoTracking().Select(g => g.Gerichtname);
            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(n => EF.Functions.Like(n, $"%{t}%"));
            }
            return await q.OrderBy(n => n).Take(20).ToListAsync();
        }

        protected async Task OnMenuChanged(int index, int which, bool forcePopulate = false)
        {
            var row = WochenFormular[index];
            var name = (which == 1 ? row.Menu1 : row.Menu2) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return;

            var info = await WeekService.FindGerichtInfoAsync(name);
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
            var check = ValidateWeekForm();
            if (!check.ok)
            {
                Message = check.msg;
                return;
            }

            try
            {
                await ReplaceWeekPreservingReservationsAsync();

                // NEU: Alle Stammdaten (Preis/Allergene) der UI jetzt definitiv in die DB schreiben
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

        // NEU: wird von SaveAsync aufgerufen, damit die DB garantiert den UI-Stand bekommt
        private async Task PersistStammdatenAsync()
        {
            foreach (var r in WochenFormular)
            {
                if (!string.IsNullOrWhiteSpace(r.Menu1))
                {
                    var n1 = r.Menu1.Trim();
                    await EnsureGerichtExistsAsync(n1);
                    await WeekService.UpdateAllergeneForGerichtAsync(n1, r.Menu1Allergene ?? string.Empty);
                    if (r.Menu1Preis.HasValue)
                    {
                        var p1 = Math.Round(r.Menu1Preis.Value, 2, MidpointRounding.AwayFromZero);
                        await WeekService.EnsurePreisForGerichtAsync(n1, p1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(r.Menu2))
                {
                    var n2 = r.Menu2.Trim();
                    await EnsureGerichtExistsAsync(n2);
                    await WeekService.UpdateAllergeneForGerichtAsync(n2, r.Menu2Allergene ?? string.Empty);
                    if (r.Menu2Preis.HasValue)
                    {
                        var p2 = Math.Round(r.Menu2Preis.Value, 2, MidpointRounding.AwayFromZero);
                        await WeekService.EnsurePreisForGerichtAsync(n2, p2);
                    }
                }
            }
        }

        private async Task ReplaceWeekPreservingReservationsAsync()
        {
            using var tx = await Db.Database.BeginTransactionAsync();

            var existingTags = await Db.Set<MenueplanTag>()
                .Where(t => t.Tag >= Monday.Date && t.Tag <= Friday.Date)
                .Include(t => t.Eintraege)
                .ToListAsync();

            var byDate = existingTags.ToDictionary(t => t.Tag.Date);

            var desired = new List<(DateTime Tag, int Gericht1, int Gericht2)>();
            foreach (var r in WochenFormular.OrderBy(x => x.Tag))
            {
                var g1 = await ResolveGerichtIdAsync(r.Menu1!.Trim());
                var g2 = await ResolveGerichtIdAsync(r.Menu2!.Trim());
                desired.Add((r.Tag.Date, g1, g2));
            }

            foreach (var d in desired)
            {
                if (!byDate.TryGetValue(d.Tag, out var tagEntity))
                {
                    tagEntity = new MenueplanTag { Tag = d.Tag };
                    Db.Set<MenueplanTag>().Add(tagEntity);

                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTag = tagEntity, PositionNr = 1, GerichtId = d.Gericht1 });
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTag = tagEntity, PositionNr = 2, GerichtId = d.Gericht2 });

                    continue;
                }

                var old1 = tagEntity.Eintraege.FirstOrDefault(e => e.PositionNr == 1);
                if (old1 is null)
                {
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 1, GerichtId = d.Gericht1 });
                }
                else if (old1.GerichtId != d.Gericht1)
                {
                    var v1 = await Db.Vorbestellungen.Where(v => v.EintragId == old1.Id).ToListAsync();
                    if (v1.Count > 0) Db.Vorbestellungen.RemoveRange(v1);
                    Db.Set<Menueplan>().Remove(old1);
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 1, GerichtId = d.Gericht1 });
                }

                var old2 = tagEntity.Eintraege.FirstOrDefault(e => e.PositionNr == 2);
                if (old2 is null)
                {
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 2, GerichtId = d.Gericht2 });
                }
                else if (old2.GerichtId != d.Gericht2)
                {
                    var v2 = await Db.Vorbestellungen.Where(v => v.EintragId == old2.Id).ToListAsync();
                    if (v2.Count > 0) Db.Vorbestellungen.RemoveRange(v2);
                    Db.Set<Menueplan>().Remove(old2);
                    Db.Set<Menueplan>().Add(new Menueplan { MenueplanTagId = tagEntity.Id, PositionNr = 2, GerichtId = d.Gericht2 });
                }
            }

            var desiredDates = desired.Select(x => x.Tag).ToHashSet();
            var toDeleteTags = existingTags.Where(t => !desiredDates.Contains(t.Tag.Date)).ToList();
            if (toDeleteTags.Count > 0)
            {
                var delEntryIds = toDeleteTags.SelectMany(t => t.Eintraege).Select(e => e.Id).ToList();
                if (delEntryIds.Count > 0)
                {
                    var v = await Db.Vorbestellungen.Where(x => delEntryIds.Contains(x.EintragId)).ToListAsync();
                    if (v.Count > 0) Db.Vorbestellungen.RemoveRange(v);
                    Db.Set<Menueplan>().RemoveRange(toDeleteTags.SelectMany(t => t.Eintraege));
                }
                Db.Set<MenueplanTag>().RemoveRange(toDeleteTags);
            }

            await Db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        private async Task<int> ResolveGerichtIdAsync(string name)
        {
            var n = (name ?? string.Empty).Trim();
            var existing = await Db.Gerichte.FirstOrDefaultAsync(x => x.Gerichtname.ToUpper() == n.ToUpper());
            if (existing is not null) return existing.Id;

            var neu = new Gericht { Gerichtname = n };
            Db.Gerichte.Add(neu);
            try
            {
                await Db.SaveChangesAsync();
                return neu.Id;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Parallel angelegt: Gewinner holen
                var winner = await Db.Gerichte.AsNoTracking()
                    .FirstAsync(x => x.Gerichtname.ToUpper() == n.ToUpper());
                return winner.Id;
            }
        }

        // eigener Context aus Factory (keine parallele Nutzung von "Db")
        private async Task<int> EnsureGerichtExistsAsync(string name)
        {
            var n = (name ?? string.Empty).Trim();
            if (n.Length == 0) return 0;

            await using var ctx = await DbFactory.CreateDbContextAsync();

            var existing = await ctx.Gerichte
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Gerichtname.ToUpper() == n.ToUpper());
            if (existing is not null) return existing.Id;

            var neu = new Gericht { Gerichtname = n };
            ctx.Gerichte.Add(neu);
            try
            {
                await ctx.SaveChangesAsync();
                return neu.Id;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var winner = await ctx.Gerichte
                    .AsNoTracking()
                    .FirstAsync(x => x.Gerichtname.ToUpper() == n.ToUpper());
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
            var data = await WeekService.LoadWeekAsync(welchewoche);

            var pdfDays = new List<PdfMenuDay>();
            decimal? commonPrice = null;

            foreach (var t in data.OrderBy(d => d.Tag))
            {
                var e1 = t.Eintraege.FirstOrDefault(e => e.PositionNr == 1)?.Gericht?.Gerichtname ?? string.Empty;
                var e2 = t.Eintraege.FirstOrDefault(e => e.PositionNr == 2)?.Gericht?.Gerichtname ?? string.Empty;

                var i1 = string.IsNullOrWhiteSpace(e1) ? null : await WeekService.FindGerichtInfoAsync(e1);
                var i2 = string.IsNullOrWhiteSpace(e2) ? null : await WeekService.FindGerichtInfoAsync(e2);

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

            var logoPath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "images", "logo.png");
            if (!File.Exists(logoPath)) logoPath = null;

            var bytes = await PdfService.BuildMenuplanPdfAsync(
                Monday.Date, Friday.Date,
                kantinenName: "MAGNA Weiz",
                title: "MENÜPLAN",
                days: pdfDays,
                mealPrice: commonPrice ?? 7.19m,
                logoPath: logoPath
            );

            var base64 = Convert.ToBase64String(bytes);
            var fileName = $"Menueplan_{Monday:yyyyMMdd}_{Friday:yyyyMMdd}.pdf";
            await JS.InvokeVoidAsync("downloadFileFromBytes", fileName, base64);
        }

        public void Dispose()
        {
            Nav.LocationChanged -= HandleLocationChanged;
        }

        private int GetUserIdFromClaims(ClaimsPrincipal user)
        {
            var idStr =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("sub") ??
                user.FindFirstValue("user_id") ??
                "0";
            return int.TryParse(idStr, out var id) ? id : 0;
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
