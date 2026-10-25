using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RaceControl.Database.Entities;

namespace RaceControl.Database;

public class RaceControlContextFactory : IDesignTimeDbContextFactory<RaceControlContext>
{
    public RaceControlContext CreateDbContext(string[] args)
    {
        var connectionString = args.FirstOrDefault();
        var options = new DbContextOptionsBuilder<RaceControlContext>()
            .UseNpgsql(
                connectionString,
                builder =>
                {
                    builder.MigrationsAssembly(typeof(RaceControlContext).Assembly.GetName().Name);
                    builder.MapEnum<MessageCategory>();
                    builder.MapEnum<MessageScope>();
                })
            .UseSnakeCaseNamingConvention()
            .Options;

        return new RaceControlContext(options);
    }
}