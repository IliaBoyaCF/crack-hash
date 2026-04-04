using Common.Abstractions;
using Common.Options;
using Common.Utils;
using Contracts.ManagerToWorker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using Worker.Abstractions;

namespace Worker.Service;

public class ResponsePublisher : RabbitMQUser, IFinalizer, IAsyncDisposable
{

    private readonly ILogger<ResponsePublisher> _logger;

    public ResponsePublisher(IOptions<ResponseQueueRabbitMQOptions> options, ILogger<ResponsePublisher> logger) : base(options.Value)
    {
        _logger = logger;
    }

    public async Task CompleteRequestAsync(CrackHashWorkerResponse response)
    {
        while (true)
        {
            _connectionReady.Wait();
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
                return;
            }
            catch (Exception ex) when (ex is PublishException || ex is OperationInterruptedException || ex is AlreadyClosedException || ex is TimeoutException)
            {
                _logger.LogError(ex, "Failed to publish response for request ({RequestId}, {PartNumber}) to queue. Retrying", response.RequestId, response.PartNumber);

            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to publish response for request ({RequestId}, {PartNumber}) to queue.", response.RequestId, response.PartNumber);
                throw;
            }
        }
    }
}
