namespace Common.Options;

public class ResponseQueueRabbitMQOptions
{
    public const string SectionName = "RabbitMQResponseQueue";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "response.direct";
    public string QueueName { get; set; } = "worker_response";
    public string RoutingKey { get; set; } = "task.response";
    public int PrefetchCount { get; set; } = 1;
    public bool Durable { get; set; } = true;
}
