using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Her setter vi opp prosjektet vårt til å bruke MVC(controllere og views)
builder.Services.AddControllersWithViews();

// Vi bestemmer at applikasjonen skal bruke engelsk som standard språk
var defaultCulture = new CultureInfo("en-US");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    // Hvis ingen språk er valgt, så brukes engelsk
    options.DefaultRequestCulture = new RequestCulture(defaultCulture);

    // Vi sier at kun engelsk støttes i denne applikasjonen 
    options.SupportedCultures = new[] { defaultCulture };
    options.SupportedUICultures = new[] { defaultCulture };
});

// Vi forteller .NET at "all kultur" (språk,datoformat osv.) skal være engelsk 
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;


var app = builder.Build();

// Dette kjøres bare hvis vi ikke er i utviklingsmodus ("i produksjon")
if (!app.Environment.IsDevelopment())
{
    //Viser en feilmelding-side hvis krasjer
    app.UseExceptionHandler("/Home/Error");

    // Sier til nettleseren at den alltid skal bruke HTTPS (sikker tilkobling)
    app.UseHsts();
}


// Aktiverer språkinnstillingene vi satte opp tidligere
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
// Setter opp standard "mellomlag" som brukes i nesten alle webapper 
app.UseStaticFiles(); // For å kunne bruke CSS, JavaScript, bilder osv.
app.UseRouting(); // Forteller appen hvordan den skal finne riktig side 
app.UseAuthorization(); // Brukes hvis vi skal ha innlogging/autorisasjon 

// Vi bestemmer hva som er "hovedsiden" til appen 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Obstacle}/{action=DataForm}/{id?}");
// Hvis du går inn på nettsiden uten å skrive noe spesielt, så sendes du til ObstacleController og DataForm-metoden.
// "Id" kan legges til i adressen, men er valgfritt. 

// Til slutt starter vi applikasjonen
app.Run();