using System;
using System.Text.Json.Serialization;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Shared.Models
{
    public class BoardMessageDto
    {
        [JsonPropertyName("messageId")]
        public int MessageId { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("isRead")]
        public bool IsRead { get; set; }

        [JsonPropertyName("priority")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessagePriority Priority { get; set; }

        [JsonPropertyName("isCurrentUser")]
        public bool IsCurrentUser { get; set; }

        [JsonPropertyName("profilePicture")]
        public string? ProfilePicture { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
} 