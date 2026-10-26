using System.Text;
using MealRotator.Services;
using Microsoft.EntityFrameworkCore;

namespace MealRotator.Tests;

public sealed class DataTransferTests : TestDatabase
{
    [Fact]
    public async Task Export_then_import_into_empty_database_restores_everything()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.AddMealAsync("Curry");
        var lasagne = (await Service.GetMealsAsync()).Single(m => m.Name == "Lasagne").Id;
        await Service.SetMealTagsAsync(lasagne, [await Service.AddTagAsync("pasta")]);
        await Service.AddTagAsync("ongebruikt");
        await Service.RecordEatenAsync(Today.AddDays(-1), "Curry");
        await Service.PlanAsync(Today.AddDays(3), "Lasagne");
        var before = await Snapshot();

        var export = await ExportAsync();
        await ClearDatabase();
        var result = await Service.ImportAsync(export);

        Assert.Equal(new ImportResult(MealsAdded: 2, TagsAdded: 2, EntriesAdded: 2), result);
        Assert.Equal(before, await Snapshot());
    }

    [Fact]
    public async Task Import_twice_adds_nothing_the_second_time()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.RecordEatenAsync(Today, "Lasagne");
        var export = await ExportAsync();

        var result = await Service.ImportAsync(export);

        Assert.Equal(new ImportResult(0, 0, 0), result);
    }

    [Fact]
    public async Task Import_keeps_existing_meals_and_days()
    {
        // An older export: "isArchived" no longer exists and is ignored.
        var file = Json($$"""
            {
              "format": "mealplanner", "version": 1, "exportedAtUtc": "2026-10-18T12:00:00Z",
              "meals": [
                { "name": "LASAGNE", "isArchived": false, "createdAtUtc": "2026-01-01T00:00:00Z" },
                { "name": "Curry", "isArchived": false, "createdAtUtc": "2026-01-01T00:00:00Z" }
              ],
              "entries": [
                { "date": "{{Today.AddDays(-1):yyyy-MM-dd}}", "meal": "Curry", "status": "Eaten" },
                { "date": "{{Today.AddDays(-2):yyyy-MM-dd}}", "meal": "Lasagne", "status": "Eaten" }
              ]
            }
            """);
        await Service.AddMealAsync("Lasagne");
        await Service.RecordEatenAsync(Today.AddDays(-1), "Lasagne");

        var result = await Service.ImportAsync(file);

        Assert.Equal(new ImportResult(MealsAdded: 1, TagsAdded: 0, EntriesAdded: 1), result);
        var rotation = await Service.GetRotationAsync();
        Assert.Equal(0, rotation.Single(r => r.Name == "Curry").EatenCount);
        Assert.Equal(2, rotation.Single(r => r.Name == "Lasagne").EatenCount);
    }

    private async Task<MemoryStream> ExportAsync()
    {
        var stream = new MemoryStream();
        await Service.ExportAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream Json(string json) => new(Encoding.UTF8.GetBytes(json));

    // Deleting the meals also deletes their entries (cascade).
    private async Task ClearDatabase()
    {
        await using var db = Factory.CreateDbContext();
        await db.Meals.ExecuteDeleteAsync();
        await db.Tags.ExecuteDeleteAsync();
    }

    private async Task<string> Snapshot()
    {
        await using var db = Factory.CreateDbContext();
        var meals = await db.Meals.OrderBy(m => m.Name)
            .Select(m => m.Name + " [" + string.Join(", ", m.Tags.OrderBy(t => t.Name).Select(t => t.Name)) + "]")
            .ToListAsync();
        var tags = await db.Tags.OrderBy(t => t.Name).Select(t => "#" + t.Name).ToListAsync();
        var entries = await db.MealEntries.OrderBy(e => e.Date)
            .Select(e => $"{e.Date:yyyy-MM-dd} {e.Meal.Name} {e.Status}").ToListAsync();
        return string.Join("\n", meals.Concat(tags).Concat(entries));
    }
}
