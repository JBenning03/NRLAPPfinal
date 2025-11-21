using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using NRLApp.Data;

// Legg til denne:
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// === DATABASE (MySQL/MariaDB via Pomelo) ===
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"[DEBUG-CONN] {cs}");


// VIKTIG: Ikke bruk AutoDetect (krever live-DB); spesifiser MariaDB-versjon uten å koble til.
var serverVersion = ServerVersion.Create(new Version(11, 0, 0), ServerType.MariaDb);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(cs, serverVersion, mySqlOptions =>
    {
        // Litt robusthet på transient feil
        mySqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
    });
});

// === IDENTITY (enkle krav i dev) ===
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireDigit = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireLowercase = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Account/Login";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.SlidingExpiration = true;
});

// === PORT-OPPSETT SOM FUNKER BÅDE LOKALT OG I DOCKER ===
var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!runningInContainer && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5099)); // lokal port
}

// === LOGGING (enkel, tydelig i konsoll) ===
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

// MVC + Session
builder.Services.AddControllersWithViews();
builder.Services.AddSession(o => o.IdleTimeout = TimeSpan.FromHours(4));

// (valgfritt) språk
var defaultCulture = new CultureInfo("en-US");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = new[] { defaultCulture };
    options.SupportedUICultures = new[] { defaultCulture };
});
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRequestLocalization(app.Services
    .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Viktig rekkefølge
app.UseAuthentication();
app.UseAuthorization();

// --- Ruter ---
app.MapGet("/", () => Results.Redirect("/Account/Login"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "obstacles",
    pattern: "obstacle/{action=Area}/{id?}",
    defaults: new { controller = "Obstacle" }
);

// === VENT PÅ DB -> MIGRER -> SEED ADMIN ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    // 1) Vent (med retry) til DB svarer
    var connected = false;
    for (int attempt = 1; attempt <= 90; attempt++) // inntil ~90 sek
    {
        try
        {
            if (await db.Database.CanConnectAsync())
            {
                connected = true;
                logger.LogInformation("DB er tilgjengelig (forsøk {Attempt}).", attempt);
                break;
            }
        }
        catch
        {
            // ignorer, prøv igjen
        }
        logger.LogInformation("Venter på DB (forsøk {Attempt}/90)...", attempt);
        await Task.Delay(1000);
    }

    if (!connected)
    {
        logger.LogError("Fikk ikke kontakt med DB innen tidsfristen. Sjekk compose.yaml og connection string.");
        throw new Exception("Database not reachable in time.");
    }

    // 2) Migrer
    await db.Database.MigrateAsync();

    // 3) Seed admin hvis tomt
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    const string adminEmail = "admin@nrl.local";
    const string adminPass = "Admin!123!";
    const string adminRole = "Admin";
    const string approverRole = "Approver";
    const string pilotRole = "Pilot";
    const string crewRole = "Crew";

    foreach (var roleName in new[] { adminRole, approverRole, pilotRole, crewRole })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    if (!userManager.Users.Any())
    {
        var admin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPass);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, adminRole);
            await userManager.AddToRoleAsync(admin, approverRole);
        }
        else
            logger.LogError("Klarte ikke å opprette admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
    }
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "http://localhost:5099";
    Console.WriteLine($"✅ Appen kjører. Åpne: {urls}/Account/Login");
});

app.Run();
