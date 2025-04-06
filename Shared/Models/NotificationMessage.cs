using System;
using BlazorApp1.Shared.Enums;

namespace BlazorApp1.Shared.Models
{
    public class NotificationMessage
    {
        public int MessageId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public MessagePriority Priority { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public bool IsCurrentUser { get; set; }

        public NotificationMessage()
        {
            Timestamp = DateTime.UtcNow;
            IsRead = false;
        }

        public static NotificationMessage FromBoardMessage(BoardMessageDto message)
        {
            return new NotificationMessage
            {
                MessageId = message.MessageId,
                Title = $"New {message.Priority} Message",
                Message = message.Content,
                Type = message.Priority.ToString(),
                UserId = message.UserId,
                UserName = message.UserName,
                Priority = message.Priority,
                Timestamp = message.Timestamp,
                IsRead = false,
                IsCurrentUser = message.IsCurrentUser
            };
        }
    }
}