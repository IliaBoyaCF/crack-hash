namespace Manager.Abstractions.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public int Capacity { get; set; }
}
