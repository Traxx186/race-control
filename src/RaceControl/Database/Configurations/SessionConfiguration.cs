using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(e => e.Id).HasName("session_pkey");
        builder.ToTable("session");

        builder.Property(e => e.Id)
            .HasMaxLength(255)
            .HasColumnName("id");
            
        builder.Property(e => e.CategoryKey)
            .HasMaxLength(32)
            .HasColumnName("category_key");
            
        builder.Property(e => e.Key)
            .HasMaxLength(32)
            .HasColumnName("key");
            
        builder.Property(e => e.Name)
            .HasMaxLength(64)
            .HasColumnName("name");
            
        builder.Property(e => e.Time)
            .HasColumnName("time");

        builder.HasOne(d => d.Category)
            .WithMany(p => p.Sessions)
            .HasForeignKey(d => d.CategoryKey)
            .HasConstraintName("fk_session_category");
    }
}