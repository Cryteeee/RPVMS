using System;
using System.Threading.Tasks;
using BlazorApp1.Server.Data;
using BlazorApp1.Server.Hubs;
using BlazorApp1.Shared;
using BlazorApp1.Shared.Models;
using BlazorApp1.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Server.Services
{
    public interface INotificationService
    {
        Task<BoardMessage> CreateNotificationAsync(int userId, string content, MessagePriority priority, string type);
        Task SendNotificationAsync(BoardMessage message, List<int> userIds);
        Task MarkAsReadAsync(int messageId, int userId);
        Task CleanupOldNotificationsAsync(int daysToKeep);
        Task<List<BoardMessage>> GetUserNotificationsAsync(int userId, int skip = 0, int take = 50);
        Task<int> GetUnreadCountAsync(int userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<BoardMessage> CreateNotificationAsync(int userId, string content, MessagePriority priority, string type)
        {
            try
            {
                var message = new BoardMessage
                {
                    UserId = userId,
                    Content = content,
                    Timestamp = DateTime.UtcNow,
                    Priority = priority,
                    IsRead = false,
                    Type = type
                };

                _context.BoardMessages.Add(message);
                await _context.SaveChangesAsync();

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
                throw;
            }
        }

        public async Task SendNotificationAsync(BoardMessage message, List<int> userIds)
        {
            try
            {
                _logger.LogInformation("Starting to send notification {MessageId} to {Count} users", message.MessageId, userIds.Count);
                
                // Create MessageRead records for all notified users
                var messageReads = userIds.Select(uId => new MessageRead
                {
                    MessageId = message.MessageId,
                    UserId = uId,
                    IsRead = false
                }).ToList();

                _context.MessageReads.AddRange(messageReads);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created MessageRead records for notification {MessageId}", message.MessageId);

                // Send real-time notifications
                var notification = new BoardMessageDto
                {
                    MessageId = message.MessageId,
                    UserId = message.UserId,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    Priority = message.Priority,
                    IsRead = false,
                    Type = message.Type
                };

                // Send to all clients (we'll filter on the client side)
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
                _logger.LogInformation("Sent SignalR notification {MessageId} to all clients", message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification {MessageId} to users", message.MessageId);
                throw;
            }
        }

        public async Task MarkAsReadAsync(int messageId, int userId)
        {
            try
            {
                var messageRead = await _context.MessageReads
                    .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId);

                if (messageRead != null)
                {
                    messageRead.IsRead = true;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read for user {UserId}", messageId, userId);
                throw;
            }
        }

        public async Task CleanupOldNotificationsAsync(int daysToKeep)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                
                // Delete old MessageRead records
                var oldMessageReads = await _context.MessageReads
                    .Where(mr => mr.Message.Timestamp < cutoffDate)
                    .ToListAsync();
                
                _context.MessageReads.RemoveRange(oldMessageReads);

                // Delete old BoardMessages
                var oldMessages = await _context.BoardMessages
                    .Where(m => m.Timestamp < cutoffDate)
                    .ToListAsync();
                
                _context.BoardMessages.RemoveRange(oldMessages);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old notifications");
                throw;
            }
        }

        public async Task<List<BoardMessage>> GetUserNotificationsAsync(int userId, int skip = 0, int take = 50)
        {
            try
            {
                return await _context.BoardMessages
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _context.MessageReads
                    .CountAsync(mr => mr.UserId == userId && !mr.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                throw;
            }
        }
    }
} 