using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NotificationService.Api.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    // Tabla para mensajes procesados (idempotencia)
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProcessedMessage>(cfg =>
        {
            cfg.HasKey(x => x.MessageId);
            cfg.Property(x => x.MessageId)
               .IsRequired()
               .HasMaxLength(100);

            cfg.Property(x => x.ProcessedAtUtc)
               .IsRequired();
        });
    }
}