namespace MealRotator.Models;

public sealed class Meal
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<MealEntry> Entries { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
}
