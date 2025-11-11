using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationService.Api.Messaging;

public sealed class RabbitMqListener : BackgroundService, IDisposable
{
    private readonly RabbitOptions _cfg;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqListener(IOptions<RabbitOptions> options)
    {
        _cfg = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create connection
        var factory = new ConnectionFactory
        {
            HostName = _cfg.Host,
            Port = _cfg.Port,
            UserName = _cfg.User,
            Password = _cfg.Pass,
            DispatchConsumersAsync = true // aunque usemos EventingBasicConsumer, dejamos preparado
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Topology: exchange + queue + binding (idempotent)
        _channel.ExchangeDeclare(exchange: _cfg.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);
        _channel.QueueDeclare(queue: _cfg.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue: _cfg.Queue, exchange: _cfg.Exchange, routingKey: _cfg.RoutingKey);

        // QoS
        _channel.BasicQos(prefetchSize: 0, prefetchCount: _cfg.Prefetch, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var payload = JsonSerializer.Deserialize<BookingCreatedEvent>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // TODO: aquí iría tu lógica real (enviar email, push, etc.)
                Console.WriteLine($"[NotificationService] Booking received => {payload?.BookingId}");

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NotificationService] Error processing message: {ex.Message}");
                // No requeue para evitar bucles infinitos por error permanente
                _channel!.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(queue: _cfg.Queue, autoAck: false, consumer: consumer);

        // Mantener vivo hasta que detengan el host
        return Task.CompletedTask;
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
