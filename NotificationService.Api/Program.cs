using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotificationService.Api.Messaging;
using NotificationService.Api.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Rabbit config
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext();
});


// DbContext para idempotencia
builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("NotificationsDb");
    options.UseNpgsql(connStr);
});

// HostedService que escucha de RabbitMQ
builder.Services.AddHostedService<RabbitMqListener>();

var app = builder.Build();

// Endpoint mínimo para ver que el servicio está vivo
app.MapGet("/", () => Results.Ok("NotificationService up"));

// ---------- Inicializar BD con reintentos ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 5;
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Notification database is ready.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Error ensuring Notification DB is created (attempt {Attempt}/{MaxAttempts}).",
                attempt, maxRetries);

            if (attempt == maxRetries)
            {
                logger.LogError(ex, "Notification DB could not be initialized. Failing startup.");
                throw; // si tras varios intentos sigue fallando, dejamos que el contenedor caiga
            }

            // Esperar unos segundos antes de reintentar
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}
// ---------- fin inicialización BD ----------
app.UseSerilogRequestLogging(); // logs estructurados por cada request

app.Run();
