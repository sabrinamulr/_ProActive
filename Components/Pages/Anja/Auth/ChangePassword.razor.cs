// Seite: ChangePassword
// Datei: Components/Pages/Anja/Auth/ChangePassword.razor.cs
using Microsoft.AspNetCore.Components;

namespace ProActive2508.Components.Pages.Anja.Auth;

public partial class ChangePassword : ComponentBase
{
    // ?ok=1 → Erfolg, ?err=xxx → Fehlercode (wie bei deinem Login)
    [SupplyParameterFromQuery(Name = "ok")] private string? Ok { get; set; }
    [SupplyParameterFromQuery(Name = "err")] private string? Err { get; set; }

    protected string? SuccessInfo { get; private set; }
    protected string? ErrorInfo { get; private set; }

    protected override void OnParametersSet()
    {
        SuccessInfo = Ok == "1" ? "Dein Passwort wurde erfolgreich geändert." : null;

        ErrorInfo = Err switch
        {
            "auth" => "Du bist nicht eingeloggt.",
            "cmp" => "Die neuen Passwörter stimmen nicht überein.",
            "pw" => "Das aktuelle Passwort ist falsch.",
            "unk" => "Unbekannter Fehler. Bitte erneut versuchen.",
            _ => null
        };
    }
}
