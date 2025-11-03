using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaddleBook.Domain.Entities;

namespace PaddleBook.Infrastructure.Persistence;

public class CourtEntityTypeConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> b)
    {
        b.ToTable("courts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Surface).IsRequired().HasMaxLength(50);
        // índices útiles más adelante:
        b.HasIndex(x => x.Name).HasDatabaseName("ix_courts_name");
    }
}
