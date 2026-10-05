using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(e => e.Id).HasName("id_pkey");
        builder.Property(e => e.Id)
            .HasIdentityOptions();

        builder.Property(e => e.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Cancelled)
            .HasDefaultValue(false);

        builder.HasOne(e => e.Championship)
            .WithMany(s => s.Sessions)
            .HasForeignKey(e => e.ChampionshipId)
            .HasConstraintName("fk_session_championship")
            .IsRequired();
    }
}