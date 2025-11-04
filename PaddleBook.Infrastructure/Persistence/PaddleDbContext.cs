using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PaddleBook.Domain.Entities;
using PaddleBook.Infrastructure.Identity;
using PaddleBook.Infrastructure.Persistence.Configurations;

namespace PaddleBook.Infrastructure.Persistence;

public class PaddleDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public PaddleDbContext(DbContextOptions<PaddleDbContext> options) : base(options) { }

    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CourtEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new BookingEntityTypeConfiguration());
    }
}
