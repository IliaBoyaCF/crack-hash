namespace Contracts.WorkerToManager;

public class TaskStatusResponse
{
    public string RequestId { get; set; }
    public int PartNumber { get; set; }
    public int PartCount { get; set; }
    public float Progress { get; set; }
}
