using Microsoft.EntityFrameworkCore;
using PaddleBook.Domain.Entities;

namespace PaddleBook.Infrastructure.Persistence;

public class PaddleDbContext : DbContext
{
    public PaddleDbContext(DbContextOptions<PaddleDbContext> options) : base(options) { }

    public DbSet<Court> Courts => Set<Court>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CourtEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
