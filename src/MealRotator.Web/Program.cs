using System.Globalization;
using MealRotator.Data;
using MealRotator.Services;
using MealRotator.Web.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;

var culture = CultureInfo.GetCultureInfo("nl-BE");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    // Bottom right, so notifications don't cover the app bar buttons.
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.VisibleStateDuration = 3500;
});

// The env var wins over appsettings.json, so a test run can point at a throwaway database.
var dataDir = Environment.GetEnvironmentVariable("MEALROTATOR_DATA_DIR")
    ?? builder.Configuration["DataDirectory"]
    ?? "App_Data";
dataDir = Path.GetFullPath(dataDir, builder.Environment.ContentRootPath);
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "mealrotator.db");

builder.Services.AddDbContextFactory<MealPlannerDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
// Scoped = per circuit, so effectively per browser tab. Do not make this a singleton.
builder.Services.AddScoped<MealPlanningService>();

var app = builder.Build();

using (var db = app.Services.GetRequiredService<IDbContextFactory<MealPlannerDbContext>>().CreateDbContext())
    db.Database.Migrate();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("nl-BE")
    .AddSupportedCultures("nl-BE")
    .AddSupportedUICultures("nl-BE"));

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
