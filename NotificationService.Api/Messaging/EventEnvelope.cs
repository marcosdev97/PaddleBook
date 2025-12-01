namespace NotificationService.Api.Messaging;

/// Envelope genérico para eventos publicados en RabbitMQ.
/// En un proyecto real esto debería vivir en un proyecto "Contracts" compartido.
public sealed class EventEnvelope<T>
{
    // Id único del mensaje para idempotencia
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    public string EventName { get; init; } = default!;
    public string CorrelationId { get; init; } = default!;
    public string? CausationId { get; init; }
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public T Payload { get; init; } = default!;
}
