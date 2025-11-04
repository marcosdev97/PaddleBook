using Microsoft.EntityFrameworkCore;
using PaddleBook.Domain.Entities;
using PaddleBook.Infrastructure.Persistence.Configurations;

namespace PaddleBook.Infrastructure.Persistence;

public class PaddleDbContext : DbContext
{
    public PaddleDbContext(DbContextOptions<PaddleDbContext> options) : base(options) { }

    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CourtEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BookingEntityTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
