namespace Manager.Abstractions.Model;

public interface IRequestInfo
{

    event EventHandler? Timeout;

    TimeSpan TimeoutInterval { get; init; }
    RequestStatus Status { get; set; }
    IEnumerable<string>? Data { get; set; }

    void AddResults(IEnumerable<string> results);

    void StartTimoutMonitoring();

}
