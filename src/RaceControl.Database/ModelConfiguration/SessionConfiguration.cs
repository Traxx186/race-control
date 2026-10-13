using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id).HasName("sessions_pkey");
        builder.ToTable("sessions");

        builder.Property(s => s.Id)
            .HasIdentityOptions();

        builder.Property(s => s.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.Type)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.StartTime)
            .IsRequired();

        builder.Property(s => s.Cancelled)
            .HasDefaultValue(false);

        builder.HasOne(s => s.Event)
            .WithMany(e => e.Sessions)
            .HasForeignKey(s => s.EventId)
            .HasConstraintName("fk_sessions_event")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}