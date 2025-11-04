using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

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

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Startside → Area
app.MapGet("/", () => Results.Redirect("/Account/Login"));

// Standard MVC-route: Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Egen kort rute for obstacle, så gamle lenker fortsatt funker
app.MapControllerRoute(
    name: "obstacles",
    pattern: "obstacle/{action=Area}/{id?}",
    defaults: new { controller = "Obstacle" });


// Info i konsollen når appen lytter (nyttig i demo)
app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "http://localhost:5099";
    Console.WriteLine($"✔️ Appen kjører. Åpne: {urls}/Account/Login");
});

app.Run();
