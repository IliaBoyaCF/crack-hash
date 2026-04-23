namespace Common.Options;

public class TaskQueueRabbitMQOptions : IRabbitMQOptions
{
    public const string SectionName = "RabbitMQTaskQueue";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "tasks.direct";
    public string QueueName { get; set; } = "worker_tasks";
    public string RoutingKey { get; set; } = "task.schedule";
    public int PrefetchCount { get; set; } = 1;
    public bool Durable { get; set; } = true;

}