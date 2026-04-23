using Common.Abstractions;
using Common.Options;
using Common.Utils;
using Contracts.ManagerToWorker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Worker.Abstractions;

namespace Worker.Service;

public class CrackRequestConsumer : RabbitMQUser, IAsyncDisposable, IHostedService
{

    private AsyncEventingBasicConsumer _consumer;
    private string _consumerTag;

    private readonly IWorker _worker;

    private readonly ILogger<CrackRequestConsumer> _logger;

    public CrackRequestConsumer(ILogger<CrackRequestConsumer> logger, IOptions<TaskQueueRabbitMQOptions> options, IWorker worker) : base(options.Value)
    {
        _logger = logger;
        _worker = worker;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{ServiceName} starting", GetType().Name);

        _consumer = new AsyncEventingBasicConsumer(_channel);

        _consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var ackAction = await ProcessMessageAsync(ea, cancellationToken);

                switch (ackAction)
                {
                    case ProcessResult.ACK:
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                        break;
                    case ProcessResult.NACK:
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                        break;
                    case ProcessResult.REJECT:
                        await _channel.BasicRejectAsync(ea.DeliveryTag, false, cancellationToken);
                        break;
                }

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

    private async Task<ProcessResult> ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        try
        {
            var body = ea.Body.ToArray();
            var xml = Encoding.UTF8.GetString(body);

            var crackRequest = XmlSerializationUtils.Deserialize<CrackHashManagerRequest>(xml);

            if (crackRequest == null)
            {
                _logger.LogWarning("Got empty message.");
                return ProcessResult.ACK;
            }

            _logger.LogInformation("Received task request: RequestId={RequestId}, PartNumber={PartNumber}/{PartCount}",
                crackRequest.RequestId, crackRequest.PartNumber, crackRequest.PartCount);

            if (_worker.RequestData.Request.Equals(crackRequest))
            {
                while (_worker.RequestData.Status != RequestStatus.COMPLETED) 
                {
                    _worker.RequestData.WaitForCompetion();
                }
                return ProcessResult.ACK;
            }
            else
            {
                if (_worker.RequestData.Status != RequestStatus.COMPLETED)
                {
                    return ProcessResult.NACK;
                }
            }

            await _worker.Schedule(crackRequest);

            return ProcessResult.ACK;

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

public enum ProcessResult
{
    ACK,
    NACK,
    REJECT,
}
