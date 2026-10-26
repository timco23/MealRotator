using MealRotator.Data;
using MealRotator.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealRotator.Tests;

/// <summary>
/// Base class: every test gets its own temporary SQLite file with the real migrations applied.
/// </summary>
public abstract class TestDatabase : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"mealplanner-test-{Guid.NewGuid():N}.db");

    protected TestDbContextFactory Factory { get; }
    protected MealPlanningService Service { get; }

    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    protected TestDatabase()
    {
        Factory = new TestDbContextFactory($"Data Source={_dbPath}");
        Service = new MealPlanningService(Factory);
    }

    public async Task InitializeAsync()
    {
        await using var db = Factory.CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
        return Task.CompletedTask;
    }
}

public sealed class TestDbContextFactory(string connectionString) : IDbContextFactory<MealPlannerDbContext>
{
    public MealPlannerDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<MealPlannerDbContext>().UseSqlite(connectionString).Options);
}
