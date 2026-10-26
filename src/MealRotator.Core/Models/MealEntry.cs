namespace MealRotator.Models;

public sealed class MealEntry
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int MealId { get; set; }
    public Meal Meal { get; set; } = null!;
    public MealEntryStatus Status { get; set; }
}
