using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.ModelConfiguration;

public class RaceControlMessageConfiguration : IEntityTypeConfiguration<RaceControlMessage>
{
    public void Configure(EntityTypeBuilder<RaceControlMessage> builder)
    {
        builder.HasKey(rcm => rcm.Id).HasName("race_control_messages_pkey");
        builder.ToTable("race_control_messages");

        builder.Property(rcm => rcm.Id)
            .HasIdentityOptions();

        builder.Property(rcm => rcm.SessionId)
            .IsRequired();

        builder.Property(rcm => rcm.Date)
            .IsRequired();

        builder.Property(rcm => rcm.Category)
            .HasColumnType("message_category")
            .IsRequired();

        builder.Property(rcm => rcm.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(rcm => rcm.Flag)
            .HasMaxLength(255);

        builder.Property(rcm => rcm.Scope)
            .HasColumnType("message_scope");

        builder.HasOne(r => r.Session)
            .WithMany(s => s.RaceControlMessages)
            .HasForeignKey(r => r.SessionId)
            .HasConstraintName("fk_race_control_messages_session")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}