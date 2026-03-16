namespace Manager.Abstractions.Model;

public class WorkerDescription
{
    public string Id { get; set; }
    public string Address { get; set; }

    public Uri Uri => new Uri(Address);
}
