using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id).HasName("events_pkey");
        builder.ToTable("events");

        builder.Property(e => e.Id)
            .HasIdentityOptions();

        builder.Property(e => e.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Location)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.MeetingKey)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.StartTime)
            .IsRequired();

        builder.Property(e => e.EndTime)
            .IsRequired();

        builder.HasOne(e => e.Championship)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.ChampionshipId)
            .HasConstraintName("fk_events_championship")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}