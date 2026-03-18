namespace Manager.Abstractions.Model;

public interface IRequestInfo : ITimeoutable
{

    /// <summary>
    /// Triggered when a request completes with status: <c>success</c>, <c>partial success</c>, or <c>error</c>.
    /// </summary>
    event EventHandler? Completed;
    Guid Id { get; }
    CrackRequest CrackRequest { get; }
    RequestStatus Status { get; set; }
    IEnumerable<string>? Data { get; set; }

    void AddResults(IEnumerable<string> results);

}
