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
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var auth = await AuthenticationStateTask;
        if (auth.User.Identity?.IsAuthenticated == true)
        {
            try
            {
                //HasDiese = await WeekService.WeekHasPlanAsync(0);
                //HasNaechste = await WeekService.WeekHasPlanAsync(1);
                //HasIn2 = await WeekService.WeekHasPlanAsync(2);
                HasDiese = false;
                HasNaechste = false;
                HasIn2 = false;
            }
            catch
            {
                HasDiese = false;
                HasNaechste = false;
                HasIn2 = false;
            }
        }
        _loading = false;
    }
}
