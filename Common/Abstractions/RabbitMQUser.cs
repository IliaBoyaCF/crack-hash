using Common.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.Abstractions;

public abstract class RabbitMQUser
{
    protected readonly IRabbitMQOptions _options;
    protected readonly IConnection _connection;
    protected readonly IChannel _channel;

    protected readonly ManualResetEventSlim _connectionReady = new ManualResetEventSlim(true);

    private bool _disposed;
    protected RabbitMQUser(IRabbitMQOptions options)
    {
        _options = options;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

        _connection.RecoverySucceededAsync += OnRecoverySucceeded;
        _connection.ConnectionShutdownAsync += OnConnectionShutdown;

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: false,
            publisherConfirmationTrackingEnabled: false);
        _channel = _connection.CreateChannelAsync(channelOptions).GetAwaiter().GetResult();

        InitializeQueueAsync().GetAwaiter().GetResult();
    }

    protected virtual async Task OnConnectionShutdown(object sender, ShutdownEventArgs @event)
    {
        _connectionReady.Reset();
    }

    protected virtual async Task OnRecoverySucceeded(object sender, AsyncEventArgs @event)
    {
        _connectionReady.Set();
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
    }

    public virtual async ValueTask DisposeAsync()
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

    }
}
