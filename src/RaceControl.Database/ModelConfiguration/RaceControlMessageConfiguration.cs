using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class RaceControlMessageConfiguration : IEntityTypeConfiguration<RaceControlMessage>
{
    public void Configure(EntityTypeBuilder<RaceControlMessage> builder)
    {
        builder.HasKey(c => c.Id).HasName("race_control_messages_pkey");
        builder.ToTable("race_control_messages");

        builder.Property(c => c.Id)
            .HasIdentityOptions();

        builder.Property(c => c.SessionId)
            .IsRequired();

        builder.Property(c => c.Date)
            .IsRequired();

        builder.Property(c => c.Category)
            .IsRequired();

        builder.Property(c => c.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(c => c.Flag)
            .HasMaxLength(255);

        builder.HasOne(r => r.Session)
            .WithMany(s => s.RaceControlMessages)
            .HasForeignKey(r => r.SessionId)
            .HasConstraintName("fk_race_control_messages_session")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}