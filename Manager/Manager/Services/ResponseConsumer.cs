using Common.Abstractions;
using Common.Options;
using Common.Utils;
using Contracts.ManagerToWorker;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Manager.Service.Services;

public class ResponseConsumer : RabbitMQUser, IHostedService, IAsyncDisposable
{

    private AsyncEventingBasicConsumer _consumer;
    private string _consumerTag;

    private readonly IRequestFinalizer _requestFinalizer;

    private readonly ILogger<ResponseConsumer> _logger;

    public ResponseConsumer(ILogger<ResponseConsumer> logger, IOptions<ResponseQueueRabbitMQOptions> options, IRequestFinalizer requestFinalizer) : base(options.Value)
    {
        _logger = logger;

        _requestFinalizer = requestFinalizer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{ServiceName} starting", GetType().Name);

        _consumer = new AsyncEventingBasicConsumer(_channel);

        _consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                await ProcessMessageAsync(ea, cancellationToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                _logger.LogInformation("Message acknowledged: {DeliveryTag}", ea.DeliveryTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {DeliveryTag}", ea.DeliveryTag);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true, cancellationToken);
            }
        };

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: _consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("{ServiceName} started successfully. Consumer tag: {ConsumerTag}", GetType().Name, _consumerTag);

    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            var body = ea.Body.ToArray();
            var xml = Encoding.UTF8.GetString(body);

            var crackResponse = XmlSerializationUtils.Deserialize<CrackHashWorkerResponse>(xml);

            if (crackResponse == null)
            {
                _logger.LogWarning("Got empty message.");
                return;
            }

            _logger.LogInformation("Received task: RequestId={RequestId}, PartNumber={PartNumber}",
                crackResponse.RequestId, crackResponse.PartNumber);

            await _requestFinalizer.ProcessWorkerResponseAsync(crackResponse);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{ServiceName} stoppping...", GetType().Name);

        if (!string.IsNullOrEmpty(_consumerTag))
        {
            await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken);
            _logger.LogInformation("Cancelled consumer: {ConsumerTag}", _consumerTag);
        }

    }
}
