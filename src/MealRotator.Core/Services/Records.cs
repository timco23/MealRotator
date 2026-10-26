using MealRotator.Models;

namespace MealRotator.Services;

public sealed record DayPlan(DateOnly Date, MealEntry? Entry);

public sealed record RotationItem(
    int Id, string Name, DateOnly? LastEaten, int EatenCount, DateOnly? NextPlanned, IReadOnlyList<string> Tags);

public sealed record MealListItem(int Id, string Name, int EatenCount, IReadOnlyList<string> Tags);

public sealed record ImportResult(int MealsAdded, int TagsAdded, int EntriesAdded);
