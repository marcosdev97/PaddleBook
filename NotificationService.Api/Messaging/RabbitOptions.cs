namespace NotificationService.Api.Messaging;

public sealed class RabbitOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string Exchange { get; set; } = "paddle.events";
    public string Queue { get; set; } = "paddle.notifications";
    public string RoutingKey { get; set; } = "booking.created";
    public ushort Prefetch { get; set; } = 10;
}
