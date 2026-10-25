using Microsoft.EntityFrameworkCore;
using RaceControl.Database.Entities;
using RaceControl.Database.ModelConfiguration;

namespace RaceControl.Database;

public class RaceControlContext(DbContextOptions<RaceControlContext> options) : DbContext(options)
{
    public DbSet<Championship> Championships { get; init; }
    public DbSet<Event> Events { get; init; }
    public DbSet<Session> Sessions { get; init; }
    public DbSet<RaceControlMessage> RacesControlMessages { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ChampionshipConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new RaceControlMessageConfiguration());

        modelBuilder.HasPostgresEnum<MessageCategory>();
        modelBuilder.HasPostgresEnum<MessageScope>();
    }
}