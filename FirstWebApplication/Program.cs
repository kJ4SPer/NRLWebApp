using FirstWebApplication.Data;
using FirstWebApplication.Entities;
using FirstWebApplication.Middleware; // For CSP (Kart-sikkerhet)
using FirstWebApplication.Services;   // For DatabaseSeeder
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// 1. KONFIGURASJON AV TJENESTER (DEPENDENCY INJECTION)
// ==============================================================================

// Legg til støtte for MVC (Controllers) og Razor Pages (Identity)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Konfigurer Databasekobling (MariaDB/MySQL)
// Henter connection string fra appsettings.json eller User Secrets
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString,
        new MariaDbServerVersion(new Version(10, 11, 0)), // Passer til MariaDB i Docker
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)
    ));

// Konfigurer Identity (Brukere og Roller)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Passord-innstillinger (Kan justeres etter behov)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // Krav om bekreftet konto før innlogging
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI() // Nødvendig for å bruke Login/Register sidene vi la til
.AddDefaultTokenProviders();

// Registrer egne tjenester
builder.Services.AddScoped<DatabaseSeeder>(); // Vår nye seeder som fikser alt data

// ==============================================================================
// 2. BYGG APPLIKASJONEN
// ==============================================================================
var app = builder.Build();

// ==============================================================================
// 3. DATABASE INITIALISERING & SEEDING
// ==============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Kjør migrasjoner automatisk (oppretter databasen hvis den mangler)
        logger.LogInformation("Kjører database-migrasjoner...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database-migrasjoner fullført.");

        // Seed data kun i utviklingsmodus (Development)
        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Starter seeding av testdata...");
            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(); // Denne metoden oppretter Roller, Org, Brukere og Hindre
            logger.LogInformation("Seeding fullført!");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "En feil oppstod under initialisering av databasen.");
    }
}

// ==============================================================================
// 4. HTTP REQUEST PIPELINE (MIDDLEWARE)
// ==============================================================================

// Content Security Policy (CSP) - Viktig for sikkerhet og kartvisning
app.UseCspMiddleware();

// Feilhåndtering
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Lar oss servere CSS, JS og bilder fra wwwroot

app.UseRouting();

// Autentisering MÅ komme før Autorisering
app.UseAuthentication(); // Hvem er du?
app.UseAuthorization();  // Hva har du lov til?

// Konfigurer ruter (URL-er)
app.MapRazorPages(); // For Identity (Login/Register)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Start applikasjonen
app.Run();