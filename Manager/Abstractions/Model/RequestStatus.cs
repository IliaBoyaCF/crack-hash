using System.Text.Json.Serialization;

namespace Manager.Abstractions.Model
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RequestStatus
    {
        IN_PROGRESS,
        IN_PROGRESS_PARTIAL_READY,
        READY_WITH_FAULTS,
        READY,
        ERROR,
    }
}
