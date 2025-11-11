namespace NotificationService.Api.Messaging;

public sealed class BookingCreatedEvent
{
    public Guid BookingId { get; init; }
    public Guid CourtId { get; init; }
    public Guid UserId { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public decimal Price { get; init; }
}
