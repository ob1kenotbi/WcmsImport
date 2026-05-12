using Microsoft.EntityFrameworkCore;
using WcmsImport.Api.Models;

namespace WcmsImport.Api.Data;

public class WcmsDbContext : DbContext
{
    public WcmsDbContext(DbContextOptions<WcmsDbContext> options) : base(options) { }

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SourceSystem).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.HasIndex(e => e.SourceSystem);
            entity.HasIndex(e => e.Status);
        });
    }
}
