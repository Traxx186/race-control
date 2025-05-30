using Microsoft.EntityFrameworkCore;
using RaceControl.Database.Entities;

namespace RaceControl.Database;

public class RaceControlContext(DbContextOptions<RaceControlContext> options) : DbContext(options)
{
    public DbSet<Category> Categories { get; init; }
    public DbSet<Session> Sessions { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Session>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder
            .Entity<Category>()
            .HasMany(e => e.Sessions)
            .WithOne(e => e.Category)
            .HasForeignKey(e => e.CategoryKey)
            .IsRequired();
    }
}