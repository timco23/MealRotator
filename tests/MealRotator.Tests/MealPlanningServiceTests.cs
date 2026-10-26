using Microsoft.EntityFrameworkCore;

namespace MealRotator.Tests;

public sealed class MealPlanningServiceTests : TestDatabase
{
    [Fact]
    public async Task GetDays_returns_one_plan_per_day()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.PlanAsync(new DateOnly(2026, 9, 16), "Lasagne");

        var week = await Service.GetDaysAsync(new DateOnly(2026, 9, 14), new DateOnly(2026, 9, 20));

        Assert.Equal(7, week.Count);
        Assert.Equal(new DateOnly(2026, 9, 14), week[0].Date);
        Assert.Equal("Lasagne", week[2].Entry?.Meal.Name);
        Assert.Null(week[0].Entry);
    }

    [Fact]
    public async Task Plan_matches_meal_regardless_of_spacing_and_case()
    {
        await Service.AddMealAsync("  Fish   tacos ");

        await Service.PlanAsync(Today.AddDays(1), "fish TACOS");

        var day = Assert.Single(await Service.GetDaysAsync(Today.AddDays(1), Today.AddDays(1)));
        Assert.Equal("Fish tacos", day.Entry?.Meal.Name);
    }

    [Fact]
    public async Task Plan_twice_on_same_date_overwrites_entry()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.AddMealAsync("Curry");

        await Service.PlanAsync(Today, "Lasagne");
        await Service.PlanAsync(Today, "Curry");

        var day = Assert.Single(await Service.GetDaysAsync(Today, Today));
        Assert.Equal("Curry", day.Entry?.Meal.Name);
    }

    [Fact]
    public async Task Plan_unknown_meal_throws()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Service.PlanAsync(Today, "Onbekend"));

        Assert.Contains("bestaand gerecht", ex.Message);
    }

    [Fact]
    public async Task Eaten_in_the_future_throws()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.PlanAsync(Today.AddDays(1), "Lasagne");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RecordEatenAsync(Today.AddDays(1), "Lasagne"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service.MarkEatenAsync(Today.AddDays(1)));
    }

    [Fact]
    public async Task RemovePlan_on_eaten_entry_throws()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.RecordEatenAsync(Today, "Lasagne");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Service.RemovePlanAsync(Today));

        Assert.Contains("Registratie verwijderen", ex.Message);
    }

    [Fact]
    public async Task DeleteMeal_removes_meal_and_entries()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.PlanAsync(Today, "Lasagne");
        await Service.RecordEatenAsync(Today.AddDays(-1), "Lasagne");
        var meal = Assert.Single(await Service.GetMealsAsync());

        await Service.DeleteMealAsync(meal.Id);

        await using var db = Factory.CreateDbContext();
        Assert.Empty(await db.Meals.ToListAsync());
        Assert.Empty(await db.MealEntries.ToListAsync());
    }

    [Fact]
    public async Task AddMeal_duplicate_with_other_case_and_spacing_throws()
    {
        await Service.AddMealAsync("Spaghetti bolognese");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service.AddMealAsync("  SPAGHETTI   Bolognese "));

        Assert.Contains("bestaat al", ex.Message);
    }

    [Fact]
    public async Task Rotation_orders_never_eaten_then_oldest_then_alphabetical()
    {
        foreach (var name in new[] { "Recent", "Oud B", "Oud A", "Nooit" })
            await Service.AddMealAsync(name);

        await Service.RecordEatenAsync(Today.AddDays(-1), "Recent");
        await Service.RecordEatenAsync(Today.AddDays(-10), "Oud B");
        await Service.RecordEatenAsync(Today.AddDays(-20), "Oud A");
        await Service.RecordEatenAsync(Today.AddDays(-30), "Recent"); // older record does not count

        var rotation = await Service.GetRotationAsync();

        Assert.Equal(["Nooit", "Oud A", "Oud B", "Recent"], rotation.Select(r => r.Name));
        Assert.Equal(Today.AddDays(-1), rotation[3].LastEaten);
        Assert.Equal(2, rotation[3].EatenCount);
    }
}
