// Datei: Components/Pages/Anja/Auth/Login.razor.cs
// Seite: Login

using Microsoft.AspNetCore.Components;

namespace ProActive2508.Components.Pages.Anja.Auth;

public partial class Login : ComponentBase
{
    // /auth/login?err=1 → Fehlermeldung
    [SupplyParameterFromQuery(Name = "err")]
    private string? Err { get; set; }

    // /auth/login?logout=1 → Info "Du wurdest abgemeldet."
    [SupplyParameterFromQuery(Name = "logout")]
    private string? Logout { get; set; }

    protected string? LoginError { get; private set; }
    protected string? LogoutInfo { get; private set; }

    protected override void OnParametersSet()
    {
        LoginError = Err == "1" ? "Personalnummer oder Passwort ist falsch." : null;
        LogoutInfo = Logout == "1" ? "Du wurdest abgemeldet." : null;
    }
}
