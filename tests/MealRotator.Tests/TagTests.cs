namespace MealRotator.Tests;

public sealed class TagTests : TestDatabase
{
    [Fact]
    public async Task AddTag_duplicate_with_other_case_and_spacing_throws()
    {
        await Service.AddTagAsync("Pasta");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Service.AddTagAsync("  PASTA "));

        Assert.Equal("Deze tag bestaat al.", ex.Message);
    }

    [Fact]
    public async Task SetMealTags_replaces_the_tags_of_a_meal()
    {
        await Service.AddMealAsync("Lasagne");
        var mealId = (await Service.GetMealsAsync()).Single().Id;
        var pasta = await Service.AddTagAsync("pasta");
        var oven = await Service.AddTagAsync("oven");
        var snel = await Service.AddTagAsync("snel");

        await Service.SetMealTagsAsync(mealId, [pasta, snel]);
        await Service.SetMealTagsAsync(mealId, [pasta, oven]);

        Assert.Equal(["oven", "pasta"], (await Service.GetMealsAsync()).Single().Tags);
    }

    [Fact]
    public async Task DeleteTag_removes_it_from_meals_but_keeps_the_meals()
    {
        await Service.AddMealAsync("Lasagne");
        var mealId = (await Service.GetMealsAsync()).Single().Id;
        var pasta = await Service.AddTagAsync("pasta");
        var oven = await Service.AddTagAsync("oven");
        await Service.SetMealTagsAsync(mealId, [pasta, oven]);

        await Service.DeleteTagAsync(pasta);

        var meal = Assert.Single(await Service.GetMealsAsync());
        Assert.Equal(["oven"], meal.Tags);
    }

    [Fact]
    public async Task Rotation_filters_by_tag()
    {
        await Service.AddMealAsync("Lasagne");
        await Service.AddMealAsync("Curry");
        var meals = await Service.GetMealsAsync();
        var pasta = await Service.AddTagAsync("pasta");
        await Service.SetMealTagsAsync(meals.Single(m => m.Name == "Lasagne").Id, [pasta]);

        var filtered = await Service.GetRotationAsync(tagId: pasta);

        Assert.Equal("Lasagne", Assert.Single(filtered).Name);
    }
}
