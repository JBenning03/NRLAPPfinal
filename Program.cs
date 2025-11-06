using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using NRLApp.Data;

var builder = WebApplication.CreateBuilder(args);
// DB: MySQL/MariaDB via Pomelo
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// Identity (enkle krav i dev – strammes inn senere)
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
// Inne i Docker settes ASPNETCORE_URLS i docker-compose (http://+:8080).
// Lokalt (VS/CLI) låser vi til http://localhost:5099 hvis ikke noe annet er satt.
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

// MVC + Session (wizard-state)
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

// ⚠️ Viktig rekkefølge
app.UseAuthentication(); // MÅ komme før UseAuthorization
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

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "http://localhost:5099";
    Console.WriteLine($"✅ Appen kjører. Åpne: {urls}/Account/Login");
});

app.Run();
