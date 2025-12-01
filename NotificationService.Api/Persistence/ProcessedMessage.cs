namespace NotificationService.Api.Persistence;

/// Entidad para registrar qué mensajes ya han sido procesados.
/// Sirve para asegurar idempotencia en el consumer.
public class ProcessedMessage
{
    public string MessageId { get; set; } = default!;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
