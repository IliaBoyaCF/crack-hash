namespace Manager.Abstractions.Options;

public class TimeoutOptions
{
    public const string SectionName = "Timeouts";

    public long RequestTimeoutMs { get; set; }
    public long WorkerTaskTimeoutMs { get; set; }

    public long TimeoutCheckIntervalMs { get; set; }

    public TimeSpan RequestTimeout => TimeSpan.FromMilliseconds(RequestTimeoutMs);

    public TimeSpan WorkerTaskTimeout => TimeSpan.FromMilliseconds(WorkerTaskTimeoutMs);

    public TimeSpan TimeoutCheckInterval => TimeSpan.FromMilliseconds(TimeoutCheckIntervalMs);


}
