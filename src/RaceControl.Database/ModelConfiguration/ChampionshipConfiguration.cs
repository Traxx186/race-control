using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class ChampionshipConfiguration : IEntityTypeConfiguration<Championship>
{
    public void Configure(EntityTypeBuilder<Championship> builder)
    {
        builder.HasKey(e => e.Id).HasName("id_pkey");
        builder.ToTable("championships");

        builder.Property(e => e.Id)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(64);
    }
}