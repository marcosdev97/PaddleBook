using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PaddleBook.Api.Messaging;

// Publisher simple que publica mensajes JSON en un exchange topic.
public class RabbitOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string Exchange { get; set; } = "paddle.events";
}

public interface IEventPublisher
{
    void Publish<T>(string routingKey, T message);
}

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly IModel _channel;
    private readonly IConnection _connection;
    private readonly string _exchange;

    public RabbitMqPublisher(IOptions<RabbitOptions> options)
    {
        var cfg = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = cfg.Host,
            Port = cfg.Port,
            UserName = cfg.User,
            Password = cfg.Pass,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _exchange = cfg.Exchange;

        // Declaramos el exchange tipo topic para routing flexible
        _channel.ExchangeDeclare(exchange: _exchange, type: ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);
    }

    public void Publish<T>(string routingKey, T message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = _channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2; // persistente

        _channel.BasicPublish(exchange: _exchange, routingKey: routingKey, basicProperties: props, body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
