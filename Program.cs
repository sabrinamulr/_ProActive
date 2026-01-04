using Microsoft.AspNetCore.Antiforgery;                              // IAntiforgery
using Microsoft.AspNetCore.Authentication;                           // SignInAsync/SignOutAsync
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;                                      // [FromForm]
using Microsoft.EntityFrameworkCore;
using ProActive2508.Components;
using ProActive2508.Data;
using ProActive2508.Models.Anja;                                     // LoginInput
using ProActive2508.Models.Entity.Anja;
using ProActive2508.Models.Entity.Anja.Kantine;
using ProActive2508.Service;
using QuestPDF.Infrastructure;
using System.Security.Claims;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

QuestPDF.Settings.License = LicenseType.Community;
builder.Services.AddSingleton<IMenuplanPdfService, MenuplanPdfService>();

builder.Services.AddScoped<Umfrage>();

// Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.Cookie.Name = ".ProActive.Auth";
        opt.LoginPath = "/auth/login";
        opt.AccessDeniedPath = "/auth/denied";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Passwort-Hasher
builder.Services.AddScoped<IPasswordHasher<Benutzer>, PasswordHasher<Benutzer>>();

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o => { o.DetailedErrors = true; });

// eigene Services
builder.Services.AddScoped<IAufgabenService, AufgabenService>();
builder.Services.AddScoped<IProjekteService, ProjekteService>();
builder.Services.AddScoped<IVormerkungService, VormerkungService>();
builder.Services.AddScoped<IKantineWeekService, KantineWeekService>();
builder.Services.AddScoped<AufgabenSessionState>();

// Session 
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);      
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;                
   
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session MUSS vor Auth/Zugriff verwendet werden
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Middleware, die Tokens erwartet/ausstellt (für <AntiforgeryToken/>)
app.UseAntiforgery();



// =============== Re-Login-Erzwingung auf neuer Browsersitzung ===============
// Wenn Benutzer authentifiziert ist, aber das Session-Flag fehlt (neue Browsersitzung),
// dann sofort abmelden und auf Login umleiten.
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value?.ToLowerInvariant() ?? "";

    // Unkritische Pfade durchlassen (statische Dateien, Framework, Login, Logout etc.)
    if (path.StartsWith("/_framework")
        || path.StartsWith("/_content")
        || path.StartsWith("/css")
        || path.StartsWith("/js")
        || path.StartsWith("/lib")
        || path.StartsWith("/images")
        || path.StartsWith("/favicon")
        || path.StartsWith("/auth/login")
        || path.StartsWith("/auth/logout")
        || path.StartsWith("/auth/changepassword"))
    {
        await next();
        return;
    }

    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var sessionFlag = ctx.Session.GetString("SessionAlive");
        if (string.IsNullOrEmpty(sessionFlag))
        {
            // Keine aktive Session (z.B. nach Browser/Tab-Schließen) → erzwinge Logout
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ctx.Response.Redirect("/auth/login?relogin=1");
            return;
        }
    }

    await next();
});

// ======== DB MIGRATE & RUNTIME-SEED ========
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Benutzer>>();

    await db.Database.MigrateAsync();

    // ---------- Benutzer ----------
    if (!await db.Benutzer.AnyAsync())
    {
        var benutzer = new List<Benutzer>
        {
            new() { Personalnummer = 1001, Email = "anna@example.com", Stufe = "Mitarbeiter",   Abteilung = "IT",      Verfuegbarkeit = 1, PasswordHash = "" },
            new() { Personalnummer = 1002, Email = "ben@example.com",  Stufe = "Projektleiter", Abteilung = "HR",      Verfuegbarkeit = 2, PasswordHash = "" },
            new() { Personalnummer = 1003, Email = "caro@example.com", Stufe = "Mitarbeiter",   Abteilung = "Sales",   Verfuegbarkeit = 3, PasswordHash = "" },
            new() { Personalnummer = 1004, Email = "dave@example.com", Stufe = "Projektleiter", Abteilung = "Finance", Verfuegbarkeit = 2, PasswordHash = "" },
            new() { Personalnummer = 1005, Email = "eva@example.com",  Stufe = "Koch",          Abteilung = "Küche",   Verfuegbarkeit = 1, PasswordHash = "" }
        };
        foreach (var u in benutzer)
            u.PasswordHash = hasher.HashPassword(u, "Passw0rd!123.");
        db.Benutzer.AddRange(benutzer);
        await db.SaveChangesAsync();

        // ---------- Projekte ----------
        var proj = new List<Projekt>
        {
            new() { BenutzerId = benutzer[0].Id, ProjektleiterId = benutzer[1].Id, AuftraggeberId = benutzer[2].Id, Status = Projektstatus.Abgeschlossen,         Phase = Projektphase.Umsetzung, Projektbeschreibung="VW" },
            new() { BenutzerId = benutzer[1].Id, ProjektleiterId = benutzer[2].Id, AuftraggeberId = benutzer[3].Id, Status = Projektstatus.Pausiert,      Phase = Projektphase.Planung , Projektbeschreibung="Mazda" },
            new() { BenutzerId = benutzer[2].Id, ProjektleiterId = benutzer[3].Id, AuftraggeberId = benutzer[4].Id, Status = Projektstatus.Abgeschlossen, Phase = Projektphase.Umsetzung, Projektbeschreibung="Audi"  },
            new() { BenutzerId = benutzer[3].Id, ProjektleiterId = benutzer[4].Id, AuftraggeberId = benutzer[0].Id, Status = Projektstatus.Abgeschlossen,         Phase = Projektphase.Planung , Projektbeschreibung = "Mercedes"},
            new() { BenutzerId = benutzer[4].Id, ProjektleiterId = benutzer[0].Id, AuftraggeberId = benutzer[1].Id, Status = Projektstatus.Pausiert,      Phase = Projektphase.Umsetzung , Projektbeschreibung = "Seat"},

        };
        db.Projekte.AddRange(proj);
        await db.SaveChangesAsync();

        // ---------- Aufgaben ----------
        const int offen = 0, bearbeitung = 1, erledigt = 2;
        var aufgaben = new List<Aufgabe>
        {
            new() { ProjektId = proj[0].Id, BenutzerId = benutzer[0].Id, Aufgabenbeschreibung = "Kickoff vorbereiten",   Faellig = new DateTime(2025, 9, 1), Phase = offen,          Erledigt = Erledigungsstatus.Offen,    ErstellVon = benutzer[1].Id },
            new() { ProjektId = proj[1].Id, BenutzerId = benutzer[1].Id, Aufgabenbeschreibung = "Anforderungen sammeln", Faellig = new DateTime(2025, 9, 2), Phase = offen,          Erledigt = Erledigungsstatus.Offen,    ErstellVon = proj[1].ProjektleiterId },
            new() { ProjektId = proj[2].Id, BenutzerId = benutzer[2].Id, Aufgabenbeschreibung = "Mockups erstellen",     Faellig = new DateTime(2025, 9, 3), Phase = offen,          Erledigt = Erledigungsstatus.Offen,    ErstellVon = benutzer[1].Id  },
            new() { ProjektId = proj[3].Id, BenutzerId = benutzer[3].Id, Aufgabenbeschreibung = "API entwerfen",         Faellig = new DateTime(2025, 9, 4), Phase = bearbeitung,    Erledigt = Erledigungsstatus.Erledigt, ErstellVon = proj[3].ProjektleiterId },
            new() { ProjektId = proj[4].Id, BenutzerId = benutzer[4].Id, Aufgabenbeschreibung = "Testplan schreiben",    Faellig = new DateTime(2025, 9, 5), Phase = erledigt,       Erledigt = Erledigungsstatus.Erledigt, ErstellVon = benutzer[1].Id  }
        };
        db.Aufgaben.AddRange(aufgaben);
        await db.SaveChangesAsync();
    }
    // ---------- Feedback-Kategorien & Fragen ----------
    if (!await db.UmfrageKategorien.AnyAsync())
    {
        var kategorien = new List<UmfrageKategorie>
    {
        new UmfrageKategorie
        {
            Name = "Allgemeine Projektzufriedenheit",
            Fragen = new List<Frage>
            {
                new Frage { Text = "Wie zufrieden bist du insgesamt mit dem Projektverlauf?" },
                new Frage { Text = "Wie zufrieden bist du mit dem Endergebnis des Projekts?" },
                new Frage { Text = "Wie klar waren die Projektziele für dich?" },
                new Frage { Text = "Wie gut war die Planung des Projekts?" }
            }
        },

        new UmfrageKategorie
        {
            Name = "Zusammenarbeit & Teamdynamik",
            Fragen = new List<Frage>
            {
                new Frage { Text = "Wie gut hat die Zusammenarbeit im Projektteam funktioniert?" },
                new Frage { Text = "Wie gut war die Kommunikation zwischen den Teammitgliedern?" },
                new Frage { Text = "Wie wertgeschätzt hast du dich im Projekt gefühlt?" },
                new Frage { Text = "Wie gut wurden Konflikte im Team gelöst?" }
            }
        },

        new UmfrageKategorie
        {
            Name = "Führung & Projektleitung",
            Fragen = new List<Frage>
            {
                new Frage { Text = "Wie gut war die Kommunikation mit der Projektleitung?" },
                new Frage { Text = "Wie klar waren die Anweisungen und Erwartungen?" },
                new Frage { Text = "Wie gut wurden Entscheidungen getroffen und kommuniziert?" },
                new Frage { Text = "Wie fair wurden Aufgaben verteilt?" }
            }
        },

        new UmfrageKategorie
        {
            Name = "Prozesse & Abläufe",
            Fragen = new List<Frage>
            {
                new Frage { Text = "Wie gut haben die Arbeitsprozesse funktioniert?" },
                new Frage { Text = "Wie gut war die Materialversorgung organisiert?" },
                new Frage { Text = "Wie gut haben Schnittstellen zu anderen Abteilungen funktioniert?" },
                new Frage { Text = "Wie realistisch waren die Zeitpläne?" }
            }
        },

        new UmfrageKategorie
        {
            Name = "Belastung & Arbeitsbedingungen",
            Fragen = new List<Frage>
            {
                new Frage { Text = "Wie hoch war deine Arbeitsbelastung während des Projekts?" },
                new Frage { Text = "Wie gut konntest du Pausen einhalten?" },
                new Frage { Text = "Wie gut war die körperliche Belastung tragbar?" },
                new Frage { Text = "Wie gut war die mentale Belastung tragbar?" }
            }
        }
    };

        db.UmfrageKategorien.AddRange(kategorien);
        await db.SaveChangesAsync();
    }

    // ---------- Allergene ----------
    if (!await db.Allergene.AnyAsync())
    {
        db.Allergene.AddRange(
            new Allergen { Kuerzel = "A" },
            new Allergen { Kuerzel = "B" },
            new Allergen { Kuerzel = "C" },
            new Allergen { Kuerzel = "G" },
            new Allergen { Kuerzel = "H" }
        );
        await db.SaveChangesAsync();
    }

    // ---------- Gerichte ----------
    if (!await db.Gerichte.AnyAsync())
    {
        db.Gerichte.AddRange(
            new Gericht { Gerichtname = "Spaghetti Bolognese" },
            new Gericht { Gerichtname = "Hühnchen Curry" },
            new Gericht { Gerichtname = "Veggie Bowl" },
            new Gericht { Gerichtname = "Rindergulasch" },
            new Gericht { Gerichtname = "Käse-Spätzle" }
        );
        await db.SaveChangesAsync();
    }

    // ---------- Preisverlauf ----------
    if (!await db.Preisverlaeufe.AnyAsync())
    {
        var gerichtByName = await db.Gerichte.ToDictionaryAsync(g => g.Gerichtname, g => g.Id);
        db.Preisverlaeufe.AddRange(
            new Preisverlauf { GerichtId = gerichtByName["Spaghetti Bolognese"], Preis = 8.50m, GueltigAb = new DateTime(2025, 9, 1) },
            new Preisverlauf { GerichtId = gerichtByName["Hühnchen Curry"], Preis = 9.20m, GueltigAb = new DateTime(2025, 9, 1) },
            new Preisverlauf { GerichtId = gerichtByName["Veggie Bowl"], Preis = 7.80m, GueltigAb = new DateTime(2025, 9, 1) },
            new Preisverlauf { GerichtId = gerichtByName["Rindergulasch"], Preis = 10.90m, GueltigAb = new DateTime(2025, 9, 1) },
            new Preisverlauf { GerichtId = gerichtByName["Käse-Spätzle"], Preis = 8.90m, GueltigAb = new DateTime(2025, 9, 1) }
        );
        await db.SaveChangesAsync();
    }

    // ---------- GerichtAllergen ----------
    if (!await db.GerichtAllergene.AnyAsync())
    {
        var allergenByKey = await db.Allergene.ToDictionaryAsync(a => a.Kuerzel, a => a.Id);
        var gerichtByName = await db.Gerichte.ToDictionaryAsync(g => g.Gerichtname, g => g.Id);

        db.GerichtAllergene.AddRange(
            new GerichtAllergen { GerichtId = gerichtByName["Spaghetti Bolognese"], AllergenId = allergenByKey["A"] },
            new GerichtAllergen { GerichtId = gerichtByName["Käse-Spätzle"], AllergenId = allergenByKey["A"] },
            new GerichtAllergen { GerichtId = gerichtByName["Käse-Spätzle"], AllergenId = allergenByKey["G"] },
            new GerichtAllergen { GerichtId = gerichtByName["Hühnchen Curry"], AllergenId = allergenByKey["C"] },
            new GerichtAllergen { GerichtId = gerichtByName["Rindergulasch"], AllergenId = allergenByKey["A"] }
        );
        await db.SaveChangesAsync();
    }

    // ---------- Menueplan-Tage ----------
    if (!await db.MenueplanTage.AnyAsync())
    {
        db.MenueplanTage.AddRange(
            new MenueplanTag { Tag = new DateTime(2025, 9, 1) },
            new MenueplanTag { Tag = new DateTime(2025, 9, 2) },
            new MenueplanTag { Tag = new DateTime(2025, 9, 3) },
            new MenueplanTag { Tag = new DateTime(2025, 9, 4) },
            new MenueplanTag { Tag = new DateTime(2025, 9, 5) }
        );
        await db.SaveChangesAsync();
    }

    // ---------- Menueplan ----------
    if (!await db.Menueplaene.AnyAsync())
    {
        var tagByDate = await db.MenueplanTage.ToDictionaryAsync(t => t.Tag.Date, t => t.Id);
        var gerichtByName = await db.Gerichte.ToDictionaryAsync(g => g.Gerichtname, g => g.Id);

        db.Menueplaene.AddRange(
            new Menueplan { MenueplanTagId = tagByDate[new DateTime(2025, 9, 1)], PositionNr = 1, GerichtId = gerichtByName["Spaghetti Bolognese"] },
            new Menueplan { MenueplanTagId = tagByDate[new DateTime(2025, 9, 1)], PositionNr = 2, GerichtId = gerichtByName["Veggie Bowl"] },
            new Menueplan { MenueplanTagId = tagByDate[new DateTime(2025, 9, 2)], PositionNr = 1, GerichtId = gerichtByName["Hühnchen Curry"] },
            new Menueplan { MenueplanTagId = tagByDate[new DateTime(2025, 9, 2)], PositionNr = 2, GerichtId = gerichtByName["Käse-Spätzle"] },
            new Menueplan { MenueplanTagId = tagByDate[new DateTime(2025, 9, 3)], PositionNr = 1, GerichtId = gerichtByName["Rindergulasch"] }
        );
        await db.SaveChangesAsync();
    }

    // ---------- Vorbestellungen ----------
    if (!await db.Vorbestellungen.AnyAsync())
    {
        var benutzerByPn = await db.Benutzer.ToDictionaryAsync(b => b.Personalnummer, b => b.Id);
        var tagByDate = await db.MenueplanTage.ToDictionaryAsync(t => t.Tag.Date, t => t.Id);
        var entries = await db.Menueplaene.ToListAsync();

        int EintragIdFor(DateTime date, int pos)
            => entries.First(e => e.MenueplanTagId == tagByDate[date.Date] && e.PositionNr == pos).Id;

        db.Vorbestellungen.AddRange(
            new Vorbestellung { BenutzerId = benutzerByPn[1001], MenueplanTagId = tagByDate[new DateTime(2025, 9, 1)], EintragId = EintragIdFor(new DateTime(2025, 9, 1), 1) },
            new Vorbestellung { BenutzerId = benutzerByPn[1002], MenueplanTagId = tagByDate[new DateTime(2025, 9, 1)], EintragId = EintragIdFor(new DateTime(2025, 9, 1), 2) },
            new Vorbestellung { BenutzerId = benutzerByPn[1003], MenueplanTagId = tagByDate[new DateTime(2025, 9, 2)], EintragId = EintragIdFor(new DateTime(2025, 9, 2), 1) },
            new Vorbestellung { BenutzerId = benutzerByPn[1004], MenueplanTagId = tagByDate[new DateTime(2025, 9, 2)], EintragId = EintragIdFor(new DateTime(2025, 9, 2), 2) },
            new Vorbestellung { BenutzerId = benutzerByPn[1005], MenueplanTagId = tagByDate[new DateTime(2025, 9, 3)], EintragId = EintragIdFor(new DateTime(2025, 9, 3), 1) }
        );
        await db.SaveChangesAsync();
    }
}
// ===========================================

// ======== AUTH ENDPOINTS ========

app.MapPost("/auth/login", async (
    HttpContext http,
    IAntiforgery antiforgery,
    AppDbContext db,
    IPasswordHasher<Benutzer> hasher,
    [FromForm] LoginInput input) =>
{
    await antiforgery.ValidateRequestAsync(http); // CSRF-Check

    var user = await db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == input.Personalnummer);
    var pwd = (input.Password ?? string.Empty).Trim();
    if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash)) return Results.Redirect("/auth/login?err=1");

    var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, pwd);
    if (verify == PasswordVerificationResult.Failed) return Results.Redirect("/auth/login?err=1");

    if (verify == PasswordVerificationResult.SuccessRehashNeeded)
    {
        var tracked = await db.Benutzer.FirstAsync(b => b.Id == user.Id);
        tracked.PasswordHash = hasher.HashPassword(tracked, pwd);
        await db.SaveChangesAsync();
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.Email) ? user.Personalnummer.ToString() : user.Email),
        new(ClaimTypes.Email, user.Email ?? string.Empty),
    };

    if (!string.IsNullOrWhiteSpace(user.Stufe))
    {
        var roles = user.Stufe.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                              .Where(r => !string.IsNullOrWhiteSpace(r))
                              .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
    var principal = new ClaimsPrincipal(identity);

    // Altes Cookie weg
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    var props = new AuthenticationProperties(); // NICHT persistent → Session-Cookie
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

    // ⬅️ WICHTIG: Session-Flag setzen → markiert laufende Browsersitzung
    http.Session.SetString("SessionAlive", "1");

    return Results.Redirect("/");
});

app.MapPost("/auth/change-password", async (HttpContext http, IAntiforgery antiforgery, AppDbContext db, IPasswordHasher<Benutzer> hasher) =>
{
    await antiforgery.ValidateRequestAsync(http);
    if (http.User?.Identity?.IsAuthenticated != true) return Results.Redirect("/changepassword?err=auth");

    var form = await http.Request.ReadFormAsync();
    var curr = form["AktuellesPasswort"].ToString();
    var neu = form["NeuesPasswort"].ToString();
    var neu2 = form["NeuesPasswortBestaetigt"].ToString();

    if (string.IsNullOrWhiteSpace(curr) || string.IsNullOrWhiteSpace(neu) || string.IsNullOrWhiteSpace(neu2)) return Results.Redirect("/auth/changepassword?err=unk");
    if (neu != neu2) return Results.Redirect("/auth/changepassword?err=cmp");
    if (neu.Length < 8) return Results.Redirect("/auth/changepassword?err=unk");

    int? currentUserId = null;
    var idStr = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? http.User.FindFirst("sub")?.Value;
    if (int.TryParse(idStr, out var idFromClaim)) currentUserId = idFromClaim;
    else
    {
        var name = http.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (int.TryParse(name, out var personalnummer))
            {
                var dbUserByPn = await db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Personalnummer == personalnummer);
                currentUserId = dbUserByPn?.Id;
            }
            else
            {
                var dbUserByMail = await db.Benutzer.AsNoTracking().FirstOrDefaultAsync(b => b.Email == name);
                currentUserId = dbUserByMail?.Id;
            }
        }
    }
    if (currentUserId is null) return Results.Redirect("/auth/changepassword?err=auth");

    var user = await db.Benutzer.FirstOrDefaultAsync(b => b.Id == currentUserId.Value);
    if (user is null) return Results.Redirect("/auth/changepassword?err=auth");

    var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, curr);
    if (verify == PasswordVerificationResult.Failed) return Results.Redirect("/auth/changepassword?err=pw");

    user.PasswordHash = hasher.HashPassword(user, neu);
    await db.SaveChangesAsync();

    return Results.Redirect("/auth/changepassword?ok=1");
}).RequireAuthorization();

app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    http.Session.Clear();

    // sendBeacon/fetch(keepalive) kann Redirects nicht folgen → 204 zurückgeben
    // Bei echter Navigation (z.B. Logout-Button im UI) kannst du redirecten:
    if (string.Equals(http.Request.Headers["Sec-Fetch-Mode"], "navigate", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Redirect("/auth/login?logout=1");
    }

    return Results.NoContent(); // 204
})
.DisableAntiforgery();

// Razor Components
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

await app.RunAsync();
