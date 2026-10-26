using MealRotator.Models;
using Microsoft.EntityFrameworkCore;

namespace MealRotator.Data;

public sealed class MealPlannerDbContext(DbContextOptions<MealPlannerDbContext> options) : DbContext(options)
{
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meal>(meal =>
        {
            meal.Property(m => m.Name).HasMaxLength(120);
            meal.Property(m => m.NormalizedName).HasMaxLength(120);
            meal.HasIndex(m => m.NormalizedName).IsUnique();

            meal.HasMany(m => m.Tags)
                .WithMany(t => t.Meals)
                .UsingEntity(link => link.ToTable("MealTags"));
        });

        modelBuilder.Entity<Tag>(tag =>
        {
            tag.Property(t => t.Name).HasMaxLength(40);
            tag.Property(t => t.NormalizedName).HasMaxLength(40);
            tag.HasIndex(t => t.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<MealEntry>(entry =>
        {
            entry.HasIndex(e => e.Date).IsUnique();
            entry.Property(e => e.Status).HasConversion<string>();

            entry.HasOne(e => e.Meal)
                .WithMany(m => m.Entries)
                .HasForeignKey(e => e.MealId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
