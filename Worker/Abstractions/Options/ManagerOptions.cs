namespace Worker.Abstractions.Options;

public class ManagerOptions
{
    public const string SectionName = "Manager";

    public string Address { get; set; }

    public Uri Uri => new Uri(Address);

}
