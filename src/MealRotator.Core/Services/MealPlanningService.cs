using System.Globalization;
using System.Text.RegularExpressions;
using MealRotator.Data;
using MealRotator.Models;
using Microsoft.EntityFrameworkCore;

namespace MealRotator.Services;

public sealed partial class MealPlanningService(IDbContextFactory<MealPlannerDbContext> factory)
{
    public const int MaxNameLength = 120;

    private const string FutureEatenError = "Een toekomstig gerecht kan niet als gegeten worden gemarkeerd.";

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // Helpers
    public static DateOnly StartOfWeek(DateOnly date) =>
        date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public static string CleanName(string name) =>
        WhitespaceRegex().Replace(name.Trim(), " ");

    public static string NormalizeName(string name) =>
        CleanName(name).ToUpper(CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private static string ValidateName(string name)
    {
        var cleaned = CleanName(name ?? "");
        if (cleaned.Length == 0)
            throw new InvalidOperationException("Voer een gerecht in.");
        if (cleaned.Length > MaxNameLength)
            throw new InvalidOperationException("Een gerechtnaam mag maximaal 120 tekens bevatten.");
        return cleaned;
    }

    // Week planning
    public async Task<IReadOnlyList<DayPlan>> GetDaysAsync(DateOnly from, DateOnly to)
    {
        await using var db = await factory.CreateDbContextAsync();

        var entries = await db.MealEntries
            .AsNoTracking()
            .Include(e => e.Meal)
            .Where(e => e.Date >= from && e.Date <= to)
            .ToDictionaryAsync(e => e.Date);

        return Enumerable.Range(0, Math.Max(0, to.DayNumber - from.DayNumber + 1))
            .Select(offset => from.AddDays(offset))
            .Select(day => new DayPlan(day, entries.GetValueOrDefault(day)))
            .ToList();
    }

    public Task PlanAsync(DateOnly date, string mealName) =>
        UpsertExistingAsync(date, mealName, MealEntryStatus.Planned);

    public Task RecordEatenAsync(DateOnly date, string mealName)
    {
        if (date > Today)
            throw new InvalidOperationException(FutureEatenError);

        return UpsertExistingAsync(date, mealName, MealEntryStatus.Eaten);
    }

    public async Task MarkEatenAsync(DateOnly date)
    {
        if (date > Today)
            throw new InvalidOperationException(FutureEatenError);

        await using var db = await factory.CreateDbContextAsync();

        var entry = await db.MealEntries.SingleOrDefaultAsync(e => e.Date == date)
            ?? throw new InvalidOperationException("Plan eerst een gerecht voordat je het als gegeten markeert.");

        entry.Status = MealEntryStatus.Eaten;
        await db.SaveChangesAsync();
    }

    public async Task RemovePlanAsync(DateOnly date)
    {
        await using var db = await factory.CreateDbContextAsync();

        var entry = await db.MealEntries.SingleOrDefaultAsync(e => e.Date == date);
        if (entry is null)
            return;

        if (entry.Status == MealEntryStatus.Eaten)
            throw new InvalidOperationException("Gebruik 'Registratie verwijderen' om een gegeten gerecht te verwijderen.");

        db.MealEntries.Remove(entry);
        await db.SaveChangesAsync();
    }

    public async Task DeleteEntryAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();

        var entry = await db.MealEntries.FindAsync(id);
        if (entry is null)
            return;

        db.MealEntries.Remove(entry);
        await db.SaveChangesAsync();
    }

    // Meals
    public async Task<IReadOnlyList<MealListItem>> GetMealsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();

        var meals = await db.Meals
            .OrderBy(m => m.Name)
            .Select(m => new MealListItem(
                m.Id,
                m.Name,
                m.Entries.Count(e => e.Status == MealEntryStatus.Eaten),
                m.Tags.Select(t => t.Name).ToList()))
            .ToListAsync();

        return meals.Select(m => m with { Tags = SortTags(m.Tags) }).ToList();
    }

    public async Task AddMealAsync(string name)
    {
        var cleaned = ValidateName(name);
        var normalized = NormalizeName(cleaned);

        await using var db = await factory.CreateDbContextAsync();

        if (await db.Meals.AnyAsync(m => m.NormalizedName == normalized))
            throw new InvalidOperationException("Dit gerecht bestaat al.");

        db.Meals.Add(new Meal { Name = cleaned, NormalizedName = normalized });
        await db.SaveChangesAsync();
    }

    public async Task DeleteMealAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();

        var meal = await db.Meals.FindAsync(id)
            ?? throw new InvalidOperationException("Gerecht niet gevonden.");

        // Its entries are removed by the cascade delete.
        db.Meals.Remove(meal);
        await db.SaveChangesAsync();
    }

    //Rotation
    public async Task<IReadOnlyList<RotationItem>> GetRotationAsync(string? search = null, int? tagId = null)
    {
        await using var db = await factory.CreateDbContextAsync();

        var today = Today;
        IQueryable<Meal> meals = db.Meals;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{CleanName(search)}%";
            meals = meals.Where(m => EF.Functions.Like(m.Name, pattern));
        }

        if (tagId is not null)
            meals = meals.Where(m => m.Tags.Any(t => t.Id == tagId));

        var items = await meals
            .Select(m => new RotationItem(
                m.Id,
                m.Name,
                m.Entries
                    .Where(e => e.Status == MealEntryStatus.Eaten)
                    .Max(e => (DateOnly?)e.Date),
                m.Entries.Count(e => e.Status == MealEntryStatus.Eaten),
                m.Entries
                    .Where(e => e.Status == MealEntryStatus.Planned && e.Date >= today)
                    .Min(e => (DateOnly?)e.Date),
                m.Tags.Select(t => t.Name).ToList()))
            .ToListAsync();

        // Never eaten first, then longest ago, then alphabetical.
        return items
            .Select(i => i with { Tags = SortTags(i.Tags) })
            .OrderBy(i => i.LastEaten.HasValue)
            .ThenBy(i => i.LastEaten)
            .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    //Internal
    private async Task UpsertExistingAsync(DateOnly date, string mealName, MealEntryStatus status)
    {
        var normalized = NormalizeName(ValidateName(mealName));

        await using var db = await factory.CreateDbContextAsync();

        var meal = await db.Meals.SingleOrDefaultAsync(m => m.NormalizedName == normalized)
            ?? throw new InvalidOperationException(
                "Kies een bestaand gerecht uit de lijst. Nieuwe gerechten voeg je toe bij Gerechten.");

        var entry = await db.MealEntries.SingleOrDefaultAsync(e => e.Date == date);
        if (entry is null)
        {
            db.MealEntries.Add(new MealEntry { Date = date, MealId = meal.Id, Status = status });
        }
        else
        {
            entry.MealId = meal.Id;
            entry.Status = status;
        }

        await db.SaveChangesAsync();
    }
}
