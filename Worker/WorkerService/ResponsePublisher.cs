using Common.Options;
using Common.Utils;
using Contracts.ManagerToWorker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using Worker.Abstractions;

namespace Worker.Service;

public class ResponsePublisher : IFinalizer, IAsyncDisposable
{

    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ResponseQueueRabbitMQOptions _options;

    private readonly ILogger<ResponsePublisher> _logger;

    private bool _disposed;
    public ResponsePublisher(IOptions<ResponseQueueRabbitMQOptions> options, ILogger<ResponsePublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        _options = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        };

        _connection = Task.Run(() => factory.CreateConnectionAsync()).Result;

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: false,
            publisherConfirmationTrackingEnabled: false);
        _channel = Task.Run(() => _connection.CreateChannelAsync(channelOptions)).Result;

        Task.Run(() => InitializeQueueAsync());
    }

    private async Task InitializeQueueAsync()
    {
        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Direct,
            durable: _options.Durable,
            autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: _options.Durable,
            exclusive: false,
            autoDelete: false);

        await _channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.RoutingKey);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: (ushort)_options.PrefetchCount,
            global: false);

        _logger.LogInformation("Queue {QueueName} bound to exchange {ExchangeName} with routing key {RoutingKey}",
            _options.QueueName, _options.ExchangeName, _options.RoutingKey);
    }

    public async Task CompleteRequestAsync(CrackHashWorkerResponse response)
    {
        try
        {

            var xml = XmlSerializationUtils.Serialize(response);
            var body = Encoding.UTF8.GetBytes(xml);

            var properties = new BasicProperties
            {
                Persistent = _options.Durable,
                ContentType = "application/xml",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Response for request ({RequestId}, {PartNumber}) published to queue", response.RequestId, response.PartNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish response for request ({RequestId}, {PartNumber}) to queue", response.RequestId, response.PartNumber);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _channel.CloseAsync();
        await _connection.CloseAsync();

        await _channel.DisposeAsync();
        await _connection.DisposeAsync();

        _disposed = true;

        _logger.LogInformation("{ClassName} disposed", GetType().Name);

    }
}
