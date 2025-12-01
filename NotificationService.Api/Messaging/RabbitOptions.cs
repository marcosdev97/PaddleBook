namespace NotificationService.Api.Messaging;


public sealed class RabbitOptions
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string User { get; set; } = default!;
    public string Pass { get; set; } = default!;
    public string Exchange { get; set; } = default!;      // main exchange
    public string Queue { get; set; } = default!;         // main queue
    public string RoutingKey { get; set; } = default!;
    public ushort Prefetch { get; set; } = 10;

    // Retry
    public string RetryExchange { get; set; } = "notifications.retry.exchange";
    public string RetryQueue { get; set; } = "notifications.retry.queue";
    public int RetryDelayMs { get; set; } = 5000;         // 5s entre reintentos
    public int MaxRetries { get; set; } = 3;

    // Dead Letter Queue (DLQ)
    public string DlqExchange { get; set; } = "notifications.dlq.exchange";
    public string DlqQueue { get; set; } = "notifications.dlq.queue";
}
