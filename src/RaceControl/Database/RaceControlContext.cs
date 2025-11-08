using Microsoft.EntityFrameworkCore;
using RaceControl.Database.Entities;
using RaceControl.Database.Configurations;

namespace RaceControl.Database;

public class RaceControlContext(DbContextOptions<RaceControlContext> options) : DbContext(options)
{
    public DbSet<Category> Categories { get; init; }
    public DbSet<Session> Sessions { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
    }
}