using System.Text.Json.Serialization;

namespace Manager.Abstractions.Model
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RequestStatus
    {
        IN_PROGRESS,
        READY,
        ERROR,
    }
}
