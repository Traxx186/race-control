using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceControl.Database.Entities;

namespace RaceControl.Database.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(e => e.Key).HasName("key_pkey");
        builder.ToTable("category");

        builder.Property(e => e.Key)
            .HasMaxLength(32)
            .HasColumnName("key");

        builder.Property(e => e.Name)
            .HasMaxLength(64)
            .HasColumnName("name");

        builder.Property(e => e.Priority)
            .HasColumnName("priority");
    }
}