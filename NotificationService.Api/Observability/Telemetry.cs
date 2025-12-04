using System.Diagnostics;

namespace NotificationService.Api.Observability;

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("NotificationService.Api");
}
