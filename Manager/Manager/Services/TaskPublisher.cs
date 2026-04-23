using Common.Abstractions;
using Common.Options;
using Common.Utils;
using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;

namespace Manager.Service.Services;

public class TaskPublisher : RabbitMQUser, ITaskScheduler, IAsyncDisposable
{

    private readonly ITaskStorage _taskStorage;

    private readonly ILogger<TaskPublisher> _logger;

    public TaskPublisher(ILogger<TaskPublisher> logger, IOptions<TaskQueueRabbitMQOptions> options, ITaskStorage taskStorage) : base(options.Value)
    {
        _logger = logger;
        _taskStorage = taskStorage;
    }

    public async Task ScheduleAsync(IEnumerable<IWorkerTask> tasks)
    {
        var taskList = tasks.ToList();

        if (!taskList.Any())
        {
            _logger.LogWarning("No tasks to schedule have been provided.");
            return;
        }

        await _taskStorage.UpsertAsync(taskList.First().Request.RequestId, taskList);

        _logger.LogInformation("Scheduling {Count} tasks to queue {QueueName}", taskList.Count, _options.QueueName);

        foreach (var task in taskList)
        {
            var message = task.Request;
            while (true)
            {
                _connectionReady.Wait();
                try
                {

                    var xml = XmlSerializationUtils.Serialize(message);
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

                    _logger.LogInformation("Task ({RequestId}, {PartNumber}/{PartCount}) published to queue", message.RequestId, message.PartNumber, message.PartCount);
                    break;
                }
                catch (Exception ex) when (ex is PublishException || ex is OperationInterruptedException || ex is AlreadyClosedException || ex is TimeoutException)
                {
                    _logger.LogError(ex, "Failed to publish task ({RequestId}, {PartNumber}/{PartCount}) to queue. Retrying.", message.RequestId, message.PartNumber, message.PartCount);

                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed to publish task ({RequestId}, {PartNumber}/{PartCount}) to queue.", message.RequestId, message.PartNumber, message.PartCount);
                    throw;
                }
            }
        }

    }
}
