using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NotificationService.Api.Persistence;

namespace NotificationService.Api.Messaging;

public sealed class RabbitMqListener : BackgroundService, IDisposable
{
    private const string RetryHeader = "x-retry-count";

    private readonly RabbitOptions _cfg;
    private readonly ILogger<RabbitMqListener> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqListener(
        IOptions<RabbitOptions> options,
        ILogger<RabbitMqListener> logger,
        IServiceScopeFactory scopeFactory)
    {
        _cfg = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _cfg.Host,
            Port = _cfg.Port,
            UserName = _cfg.User,
            Password = _cfg.Pass,
            DispatchConsumersAsync = true
        };

        // ---------- Intentar conectar a RabbitMQ con reintentos ----------
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                _logger.LogInformation("Trying to connect to RabbitMQ (attempt {Attempt}/{MaxAttempts})...",
                    attempt, maxAttempts);

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _logger.LogInformation("Connected to RabbitMQ successfully.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not connect to RabbitMQ (attempt {Attempt}/{MaxAttempts}).",
                    attempt, maxAttempts);

                if (attempt == maxAttempts)
                {
                    _logger.LogError(ex,
                        "RabbitMQ could not be reached after {MaxAttempts} attempts. Stopping listener.",
                        maxAttempts);
                    return; // salimos del BackgroundService, mejor que reventar
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        if (_channel is null || stoppingToken.IsCancellationRequested)
        {
            // No hemos conseguido canal o se ha cancelado el token
            return;
        }

        // 1) Exchanges
        _channel.ExchangeDeclare(_cfg.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.ExchangeDeclare(_cfg.RetryExchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.ExchangeDeclare(_cfg.DlqExchange, ExchangeType.Topic, durable: true, autoDelete: false);

        // 2) Cola principal
        _channel.QueueDeclare(
            queue: _cfg.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                // Si alguien hace BasicNack(requeue:false) a esta cola sin republish manual,
                // podría ir a un DLX. De momento no lo usamos para retries.
            });

        _channel.QueueBind(_cfg.Queue, _cfg.Exchange, _cfg.RoutingKey);

        // 3) Cola de retry (con TTL y DLX de vuelta al exchange principal)
        _channel.QueueDeclare(
            queue: _cfg.RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = _cfg.RetryDelayMs,       // tiempo de espera antes de volver
                ["x-dead-letter-exchange"] = _cfg.Exchange   // al expirar, vuelve al exchange principal
            });

        _channel.QueueBind(_cfg.RetryQueue, _cfg.RetryExchange, _cfg.RoutingKey);

        // 4) Cola DLQ (solo almacena mensajes muertos)
        _channel.QueueDeclare(
            queue: _cfg.DlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(_cfg.DlqQueue, _cfg.DlqExchange, _cfg.RoutingKey);

        // QoS
        _channel.BasicQos(prefetchSize: 0, prefetchCount: _cfg.Prefetch, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                var envelope = JsonSerializer.Deserialize<EventEnvelope<BookingCreatedEvent>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (envelope is null)
                {
                    _logger.LogWarning("Received null or invalid envelope. Raw message: {Json}", json);
                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                using var logScope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = envelope.CorrelationId,
                    ["EventName"] = envelope.EventName,
                    ["MessageId"] = envelope.MessageId
                });

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

                // Idempotencia: ¿ya procesamos este mensaje?
                var alreadyProcessed = await db.ProcessedMessages.FindAsync(envelope.MessageId);
                if (alreadyProcessed is not null)
                {
                    _logger.LogInformation("Message {MessageId} already processed. Skipping.", envelope.MessageId);
                    _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                var payload = envelope.Payload;

                // Lógica de negocio (simplificada)
                _logger.LogInformation(
                    "Processing booking notification. BookingId={BookingId}, CourtId={CourtId}, Start={Start}, End={End}",
                    payload.BookingId,
                    payload.CourtId,
                    payload.StartUtc,
                    payload.EndUtc);

                // TODO: enviar email / push real aquí

                // Marcar como procesado (idempotencia)
                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    MessageId = envelope.MessageId,
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(stoppingToken);

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Cálculo de reintentos
                var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object>();
                int currentRetry = 0;

                if (headers.TryGetValue(RetryHeader, out var retryObj))
                {
                    try
                    {
                        // Rabbit devuelve el header como byte[] normalmente
                        if (retryObj is byte[] raw)
                        {
                            var str = Encoding.UTF8.GetString(raw);
                            int.TryParse(str, out currentRetry);
                        }
                        else if (retryObj is int i)
                        {
                            currentRetry = i;
                        }
                        else if (retryObj is long l)
                        {
                            currentRetry = (int)l;
                        }
                    }
                    catch { /* si peta, dejamos retry=0 */ }
                }

                _logger.LogError(ex,
                    "Error processing message. CurrentRetry={Retry}/{MaxRetries}",
                    currentRetry, _cfg.MaxRetries);

                if (currentRetry < _cfg.MaxRetries)
                {
                    // Republicar a la cola de retry con retryCount+1
                    var props = _channel!.CreateBasicProperties();
                    props.Persistent = true;

                    var newHeaders = new Dictionary<string, object>(headers)
                    {
                        [RetryHeader] = (currentRetry + 1).ToString()
                    };
                    props.Headers = newHeaders;

                    _channel.BasicPublish(
                        exchange: _cfg.RetryExchange,
                        routingKey: _cfg.RoutingKey,
                        basicProperties: props,
                        body: ea.Body);

                    _logger.LogWarning("Message sent to retry exchange. Retry={Retry}", currentRetry + 1);

                    // Acknowledge del mensaje original (ya hemos creado una copia para reintento)
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                else
                {
                    // Máximos reintentos alcanzados: mandar a DLQ
                    var props = _channel!.CreateBasicProperties();
                    props.Persistent = true;
                    props.Headers = ea.BasicProperties.Headers;

                    _channel.BasicPublish(
                        exchange: _cfg.DlqExchange,
                        routingKey: _cfg.RoutingKey,
                        basicProperties: props,
                        body: ea.Body);

                    _logger.LogError(
                        "Message moved to DLQ after {MaxRetries} retries.",
                        _cfg.MaxRetries);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
            }
        };

        _channel.BasicConsume(queue: _cfg.Queue, autoAck: false, consumer: consumer);

        await Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        finally
        {
            base.Dispose();
        }
    }
}
