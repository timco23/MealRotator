using MealRotator.Models;
using Microsoft.EntityFrameworkCore;

namespace MealRotator.Services;

public sealed record TagListItem(int Id, string Name, int MealCount);

public sealed partial class MealPlanningService
{
    public const int MaxTagLength = 40;

    // ---------- Tags ----------

    public async Task<IReadOnlyList<TagListItem>> GetTagsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();

        var tags = await db.Tags
            .Select(t => new TagListItem(t.Id, t.Name, t.Meals.Count))
            .ToListAsync();

        return tags.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <returns>The id of the new tag.</returns>
    public async Task<int> AddTagAsync(string name)
    {
        var cleaned = ValidateTagName(name);
        var normalized = NormalizeName(cleaned);

        await using var db = await factory.CreateDbContextAsync();

        if (await db.Tags.AnyAsync(t => t.NormalizedName == normalized))
            throw new InvalidOperationException("Deze tag bestaat al.");

        var tag = new Tag { Name = cleaned, NormalizedName = normalized };
        db.Tags.Add(tag);
        await db.SaveChangesAsync();
        return tag.Id;
    }

    public async Task RenameTagAsync(int id, string name)
    {
        var cleaned = ValidateTagName(name);
        var normalized = NormalizeName(cleaned);

        await using var db = await factory.CreateDbContextAsync();

        var tag = await db.Tags.FindAsync(id)
            ?? throw new InvalidOperationException("Tag niet gevonden.");

        // Only changing the case or spacing of its own name is fine.
        if (await db.Tags.AnyAsync(t => t.NormalizedName == normalized && t.Id != id))
            throw new InvalidOperationException("Deze tag bestaat al.");

        tag.Name = cleaned;
        tag.NormalizedName = normalized;
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes the tag and removes it from its meals; the meals themselves stay.</summary>
    public async Task DeleteTagAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();

        var tag = await db.Tags.FindAsync(id);
        if (tag is null)
            return;

        db.Tags.Remove(tag);
        await db.SaveChangesAsync();
    }

    /// <summary>Replaces the tags of a meal with <paramref name="tagIds"/>.</summary>
    public async Task SetMealTagsAsync(int mealId, IEnumerable<int> tagIds)
    {
        var ids = tagIds.ToHashSet();

        await using var db = await factory.CreateDbContextAsync();

        var meal = await db.Meals
            .Include(m => m.Tags)
            .SingleOrDefaultAsync(m => m.Id == mealId)
            ?? throw new InvalidOperationException("Gerecht niet gevonden.");

        var tags = await db.Tags.Where(t => ids.Contains(t.Id)).ToListAsync();
        if (tags.Count != ids.Count)
            throw new InvalidOperationException("Tag niet gevonden.");

        // Only add and remove the difference, so unchanged links are left alone.
        meal.Tags.RemoveAll(t => !ids.Contains(t.Id));
        var added = tags.Where(t => meal.Tags.All(existing => existing.Id != t.Id)).ToList();
        meal.Tags.AddRange(added);
        await db.SaveChangesAsync();
    }

    private static string ValidateTagName(string name)
    {
        var cleaned = CleanName(name ?? "");
        if (cleaned.Length == 0)
            throw new InvalidOperationException("Voer een tagnaam in.");
        if (cleaned.Length > MaxTagLength)
            throw new InvalidOperationException("Een tag mag maximaal 40 tekens bevatten.");
        return cleaned;
    }

    private static IReadOnlyList<string> SortTags(IEnumerable<string> names) =>
        names.Order(StringComparer.CurrentCultureIgnoreCase).ToList();
}
