using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class ChampionshipConfiguration : IEntityTypeConfiguration<Championship>
{
    public void Configure(EntityTypeBuilder<Championship> builder)
    {
        builder.HasKey(c => c.Id).HasName("championships_pkey");
        builder.ToTable("championships");

        builder.Property(c => c.Id)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(c => c.Latency)
            .HasDefaultValue(new TimeSpan(0, 0, 35));
    }
}