using System.Text.Json;
using System.Text.Json.Serialization;
using MealRotator.Models;
using Microsoft.EntityFrameworkCore;

namespace MealRotator.Services;

/// <summary>
/// Root of an export file. Meals and tags are referenced by name, not by id, so files can be merged.
/// <see cref="Tags"/> lists every tag, also unused ones; files from before tags existed have none.
/// </summary>
public sealed record ExportFile(
    string Format,
    int Version,
    DateTime ExportedAtUtc,
    IReadOnlyList<ExportMeal> Meals,
    IReadOnlyList<ExportEntry> Entries,
    IReadOnlyList<string>? Tags = null);

public sealed record ExportMeal(string Name, DateTime CreatedAtUtc, IReadOnlyList<string>? Tags = null);

public sealed record ExportEntry(DateOnly Date, string Meal, MealEntryStatus Status);

public sealed partial class MealPlanningService
{
    public const string ExportFormat = "mealplanner";

    // Bump only for breaking changes; new optional fields are ignored by older readers anyway.
    public const int ExportVersion = 1;

    private const string InvalidFileError = "Dit bestand is geen geldige export van de Maaltijdplanner.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public async Task ExportAsync(Stream output)
    {
        await using var db = await factory.CreateDbContextAsync();

        var meals = await db.Meals
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new ExportMeal(m.Name, m.CreatedAtUtc, m.Tags.Select(t => t.Name).ToList()))
            .ToListAsync();

        var entries = await db.MealEntries
            .AsNoTracking()
            .OrderBy(e => e.Date)
            .Select(e => new ExportEntry(e.Date, e.Meal.Name, e.Status))
            .ToListAsync();

        var tags = await db.Tags.Select(t => t.Name).ToListAsync();

        // SQLite returns DateTimeKind.Unspecified; mark as UTC so the file carries a "Z".
        var file = new ExportFile(
            ExportFormat,
            ExportVersion,
            DateTime.UtcNow,
            meals.Select(m => m with
            {
                CreatedAtUtc = DateTime.SpecifyKind(m.CreatedAtUtc, DateTimeKind.Utc),
                Tags = SortTags(m.Tags!)
            }).ToList(),
            entries,
            SortTags(tags));

        await JsonSerializer.SerializeAsync(output, file, JsonOptions);
    }

    /// <summary>
    /// Merges an export into the database: missing tags, meals and days are added; existing meals and
    /// days that are already filled in stay as they are. Nothing is saved when the file has an error.
    /// </summary>
    public async Task<ImportResult> ImportAsync(Stream input)
    {
        var file = await ReadExportAsync(input);

        await using var db = await factory.CreateDbContextAsync();

        var tags = await db.Tags.ToDictionaryAsync(t => t.NormalizedName);
        var meals = await db.Meals.ToDictionaryAsync(m => m.NormalizedName);
        var takenDates = (await db.MealEntries.Select(e => e.Date).ToListAsync()).ToHashSet();
        int tagsAdded = 0, mealsAdded = 0, entriesAdded = 0;

        // Returns the tag with this name, adding it first when it doesn't exist yet.
        Tag GetOrAddTag(string name)
        {
            var cleaned = ValidateTagName(name);
            var normalized = NormalizeName(cleaned);
            if (!tags.TryGetValue(normalized, out var tag))
            {
                tag = new Tag { Name = cleaned, NormalizedName = normalized };
                db.Tags.Add(tag);
                tags[normalized] = tag;
                tagsAdded++;
            }
            return tag;
        }

        foreach (var name in file.Tags ?? [])
            GetOrAddTag(name);

        foreach (var meal in file.Meals)
        {
            var cleaned = ValidateName(meal.Name);
            var normalized = NormalizeName(cleaned);
            if (meals.ContainsKey(normalized))
                continue;

            var added = new Meal
            {
                Name = cleaned,
                NormalizedName = normalized,
                CreatedAtUtc = meal.CreatedAtUtc.ToUniversalTime(),
                Tags = (meal.Tags ?? []).Select(GetOrAddTag).Distinct().ToList()
            };
            db.Meals.Add(added);
            meals[normalized] = added;
            mealsAdded++;
        }

        foreach (var entry in file.Entries)
        {
            if (!meals.TryGetValue(NormalizeName(entry.Meal), out var meal))
                throw new InvalidOperationException($"Het bestand verwijst naar een onbekend gerecht: '{entry.Meal}'.");
            if (entry.Status == MealEntryStatus.Eaten && entry.Date > Today)
                throw new InvalidOperationException(FutureEatenError);
            if (!takenDates.Add(entry.Date))
                continue;

            db.MealEntries.Add(new MealEntry { Date = entry.Date, Meal = meal, Status = entry.Status });
            entriesAdded++;
        }

        await db.SaveChangesAsync();

        return new ImportResult(mealsAdded, tagsAdded, entriesAdded);
    }

    private static async Task<ExportFile> ReadExportAsync(Stream input)
    {
        ExportFile? file;
        try
        {
            file = await JsonSerializer.DeserializeAsync<ExportFile>(input, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(InvalidFileError, ex);
        }

        if (file is null || file.Format != ExportFormat || file.Version < 1)
            throw new InvalidOperationException(InvalidFileError);
        if (file.Version > ExportVersion)
            throw new InvalidOperationException(
                "Dit bestand is gemaakt met een nieuwere versie van de Maaltijdplanner. Werk de app eerst bij.");

        return file;
    }
}
