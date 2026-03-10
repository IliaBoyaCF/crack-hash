namespace Manager.Abstractions.Options;

public class RequestQueueOptions
{
    public const string SectionName = "RequestQueue";

    public int Capacity { get; set; }
}
