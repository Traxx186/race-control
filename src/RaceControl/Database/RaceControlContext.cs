using Microsoft.EntityFrameworkCore;
using RaceControl.Database.Entities;

namespace RaceControl.Database;

public class RaceControlContext(DbContextOptions<RaceControlContext> options) : DbContext(options)
{
    public DbSet<Category> Categories { get; init; }
    public DbSet<Session> Sessions { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Key).HasName("key_pkey");
            entity.ToTable("category");

            entity.Property(e => e.Key)
                .HasMaxLength(32)
                .HasColumnName("key");
            
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .HasColumnName("name");
            
            entity.Property(e => e.Priority)
                .HasColumnName("priority");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("session_pkey");
            entity.ToTable("session");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .HasColumnName("id");
            
            entity.Property(e => e.CategoryKey)
                .HasMaxLength(32)
                .HasColumnName("category_key");
            
            entity.Property(e => e.Key)
                .HasMaxLength(32)
                .HasColumnName("key");
            
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .HasColumnName("name");
            
            entity.Property(e => e.Time)
                .HasColumnName("time");

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.CategoryKey)
                .HasConstraintName("fk_session_category");
        });
    }
}