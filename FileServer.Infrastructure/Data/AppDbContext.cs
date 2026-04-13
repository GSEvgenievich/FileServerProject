using FileServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileServer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FileRecord> FileRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoredName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Bucket).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Thumbnail).HasColumnType("bytea"); 
            entity.HasIndex(e => e.UploadedAt);
        });
    }
}