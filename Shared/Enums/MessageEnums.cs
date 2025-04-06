using System.Text.Json.Serialization;

namespace BlazorApp1.Shared.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MessagePriority
    {
        Normal,
        Announcement,
        Emergency,
        Priority
    }
} 