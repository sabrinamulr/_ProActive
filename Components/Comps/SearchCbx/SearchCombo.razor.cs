// Datei: Components/Comps/SearchCbx/SearchCombo.razor.cs
// Seite: SearchCombo

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ProActive2508.Components.Comps.SearchCbx
{
    public partial class SearchCombo<TItem>
    {
        [Parameter] public string Id { get; set; } = $"searchcombo_{Guid.NewGuid()}";
        [Parameter] public string Placeholder { get; set; } = "Bitte wählen…";
        [Parameter] public List<TItem> Items { get; set; } = [];
        [Parameter] public Func<TItem, string>? LabelSelector { get; set; }

        // Auswahl an Parent
        [Parameter] public EventCallback<TItem> OnItemSelected { get; set; }

        // 2-way Suche
        [Parameter] public string Search { get; set; } = string.Empty;
        [Parameter] public EventCallback<string> SearchChanged { get; set; }

        // Wird gefeuert, wenn Text final bestätigt wurde (Klick, Enter, Blur)
        [Parameter] public EventCallback<string> TextCommitted { get; set; }

        private List<TItem> _filtered = [];
        private bool _showList = false;
        private CancellationTokenSource? _blurCts;
        private string _search = string.Empty;
        private string CurrentSearch { get; set; } = string.Empty;
        private List<TItem> _lastItems = [];

        // ⬇️ Neu: Doppel-Commit vermeiden & Blur nach Enter/Klick unterdrücken
        private bool _suppressNextBlurCommit = false;
        private string _lastCommittedText = string.Empty;

        protected override async Task OnParametersSetAsync()
        {
            bool itemsChanged = !ReferenceEquals(_lastItems, Items);
            if (itemsChanged || CurrentSearch != Search)
            {
                CurrentSearch = Search;
                _search = Search;
                _lastItems = Items;
                FilterItems();
                await InvokeAsync(StateHasChanged);
            }
        }

        private void FilterItems()
        {
            _filtered = string.IsNullOrWhiteSpace(_search)
                ? new List<TItem>(Items)
                : Items.Where(item => (LabelSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty)
                        .Contains(_search, StringComparison.OrdinalIgnoreCase))
                       .ToList();
        }

        private async Task OnInput(ChangeEventArgs e)
        {
            CurrentSearch = e.Value?.ToString() ?? string.Empty;
            await UpdateSearch(CurrentSearch);
        }

        private async Task UpdateSearch(string value)
        {
            if (_search == value) return;
            _search = value;
            FilterItems();
            await SearchChanged.InvokeAsync(value);
            await InvokeAsync(StateHasChanged);
        }

        private async Task ShowList(FocusEventArgs _)
        {
            _blurCts?.Cancel();
            _showList = true;
            FilterItems();
            await InvokeAsync(StateHasChanged);
        }

        // Blur: Liste schließen + Text „committed“ melden (entprellt)
        private async Task OnBlur(FocusEventArgs _)
        {
            _blurCts = new CancellationTokenSource();
            var token = _blurCts.Token;
            try
            {
                // kurzer Delay, damit Klick auf Listeneintrag zuerst verarbeitet wird
                await Task.Delay(200, token);
                if (token.IsCancellationRequested) return;

                _showList = false;
                await InvokeAsync(StateHasChanged);

                // Falls kurz zuvor Enter/Klick bestätigt hat → Blur nicht nochmal committen
                if (_suppressNextBlurCommit)
                {
                    _suppressNextBlurCommit = false; // nur einmal unterdrücken
                    return;
                }

                await CommitAsync(CurrentSearch);
            }
            catch (TaskCanceledException)
            {
                // ignorieren
            }
        }

        private async Task SelectItemAsync(TItem item)
        {
            _blurCts?.Cancel();                // geplantes Blur abbrechen
            _suppressNextBlurCommit = true;    // nach Click kein zusätzlicher Blur-Commit

            var newSearch = LabelSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;
            CurrentSearch = newSearch;

            await UpdateSearch(newSearch);
            _showList = false;

            if (OnItemSelected.HasDelegate)
                await OnItemSelected.InvokeAsync(item);

            await CommitAsync(CurrentSearch);   // dedupliziert
            await InvokeAsync(StateHasChanged);
        }

        // ENTER bestätigt
        private async Task OnKeyDown(KeyboardEventArgs e)
        {
            if (!string.Equals(e.Key, "Enter", StringComparison.OrdinalIgnoreCase))
                return;

            if (_filtered.Count > 0)
            {
                // exakter Treffer?
                var idx = _filtered.FindIndex(item =>
                    string.Equals(
                        LabelSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty,
                        CurrentSearch,
                        StringComparison.OrdinalIgnoreCase));

                if (idx < 0) idx = 0;
                await SelectItemAsync(_filtered[idx]); // SelectItemAsync handhabt Commit & Blur-Unterdrückung
                return;
            }

            // Kein Treffer in der Liste → freien Text übernehmen (nur sinnvoll bei string)
            if (typeof(TItem) == typeof(string))
            {
                _suppressNextBlurCommit = true;  // Blur danach nicht committen
                var text = CurrentSearch;

                if (OnItemSelected.HasDelegate)
                    await OnItemSelected.InvokeAsync((TItem)(object)text);

                await CommitAsync(text);          // dedupliziert
                _showList = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        // ---- Helfer: dedupliziertes Commit ----
        private static bool TextEquals(string a, string b) =>
            string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

        private bool ShouldCommit(string text) =>
            !string.IsNullOrWhiteSpace(text) && !TextEquals(text, _lastCommittedText);

        private async Task CommitAsync(string text)
        {
            var t = (text ?? string.Empty).Trim();
            if (!ShouldCommit(t)) return;

            _lastCommittedText = t;
            if (TextCommitted.HasDelegate)
                await TextCommitted.InvokeAsync(t);
        }
    }
}
