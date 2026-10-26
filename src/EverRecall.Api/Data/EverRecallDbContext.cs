using EverRecall.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EverRecall.Api.Data;

public class EverRecallDbContext : DbContext
{
    public EverRecallDbContext(DbContextOptions<EverRecallDbContext> options) : base(options) { }

    public DbSet<MemoryEvent> MemoryEvents => Set<MemoryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MemoryEvent>(e =>
        {
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => x.ObjectName);
        });
    }
}
