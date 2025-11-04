using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaddleBook.Domain.Entities;

namespace PaddleBook.Infrastructure.Persistence.Configurations;

public class BookingEntityTypeConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        b.ToTable("bookings");
        b.HasKey(x => x.Id);

        b.Property(x => x.CustomerName)
            .IsRequired()
            .HasMaxLength(100);

        b.Property(x => x.StartTime).IsRequired();
        b.Property(x => x.EndTime).IsRequired();

        // Relación Court 1:N Bookings
        b.HasOne(x => x.Court)
            .WithMany() // más adelante podríamos agregar Courts.Bookings
            .HasForeignKey(x => x.CourtId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CourtId, x.StartTime, x.EndTime });
    }
}
