using NotificationService.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Bind Rabbit options
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));

// Register background listener
builder.Services.AddHostedService<RabbitMqListener>();

// Minimal API just to check liveness
var app = builder.Build();
app.MapGet("/", () => Results.Ok("NotificationService up"));
app.Run();
