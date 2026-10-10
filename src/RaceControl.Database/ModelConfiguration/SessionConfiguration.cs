using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(e => e.Id).HasName("sessions_pkey");
        builder.ToTable("sessions");

        builder.Property(e => e.Id)
            .HasIdentityOptions();

        builder.Property(e => e.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Event)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Cancelled)
            .HasDefaultValue(false);

        builder.HasOne(s => s.Championship)
            .WithMany(c => c.Sessions)
            .HasForeignKey(s => s.ChampionshipId)
            .HasConstraintName("fk_sessions_championship")
            .IsRequired();
    }
}