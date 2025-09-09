// Datei: Shared/NavMenu.razor.cs
// Seite: NavMenu (Code-Behind)

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProActive2508.Service;

namespace ProActive2508.Components.Layout;

public partial class NavMenu : ComponentBase
{
    private bool _loading = true;
    private bool HasDiese, HasNaechste, HasIn2;

    [Inject] private IKantineWeekService WeekService { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthenticationStateTask;
        if (auth.User.Identity?.IsAuthenticated == true)
        {
            HasDiese = await WeekService.WeekHasPlanAsync(0);
            HasNaechste = await WeekService.WeekHasPlanAsync(1);
            HasIn2 = await WeekService.WeekHasPlanAsync(2);
        }
        _loading = false;
    }
}
