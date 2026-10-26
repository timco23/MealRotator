namespace MealRotator.Models;

public sealed class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public List<Meal> Meals { get; set; } = [];
}
