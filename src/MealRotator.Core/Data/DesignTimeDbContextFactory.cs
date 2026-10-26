using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MealRotator.Data;

// Only used by `dotnet ef` (migrations); the app configures the context itself.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MealPlannerDbContext>
{
    public MealPlannerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseSqlite("Data Source=design.db")
            .Options;
        return new MealPlannerDbContext(options);
    }
}
