using Microsoft.AspNetCore.SignalR;
using BlazorApp1.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BlazorApp1.Server.Hubs
{
    [Authorize]
    public class BoardMessageHub : Hub
    {
        private readonly ILogger<BoardMessageHub> _logger;
        private static readonly Dictionary<string, string> _activeConnections = new();

        public BoardMessageHub(ILogger<BoardMessageHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            _activeConnections[Context.ConnectionId] = userId ?? "unknown";
            
            _logger.LogInformation(
                "Client connected - ConnectionId: {ConnectionId}, UserId: {UserId}, UserName: {UserName}, Role: {Role}, Total Active: {ActiveCount}",
                Context.ConnectionId, userId, userName, userRole, _activeConnections.Count);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            _activeConnections.Remove(Context.ConnectionId);

            _logger.LogInformation(
                "Client disconnected - ConnectionId: {ConnectionId}, UserId: {UserId}, UserName: {UserName}, Error: {Error}, Remaining Active: {ActiveCount}",
                Context.ConnectionId, userId, userName, exception?.Message, _activeConnections.Count);

            if (exception != null)
            {
                _logger.LogError(exception, "Disconnection error for user {UserName}", userName);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(BoardMessageDto message)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
                var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogInformation(
                    "Message send attempt - From UserId: {UserId}, UserName: {UserName}, Role: {Role}, MessageId: {MessageId}, Content: {Content}, Priority: {Priority}",
                    userId, userName, userRole, message.MessageId, message.Content, message.Priority);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName))
                {
                    _logger.LogError("Authentication failed - Missing user information in connection context");
                    throw new HubException("User not properly authenticated");
                }

                // Verify the message sender
                if (message.UserId != int.Parse(userId))
                {
                    _logger.LogError("Message sender verification failed - UserId mismatch");
                    throw new HubException("Message sender verification failed");
                }

                var activeConnections = _activeConnections.Count;
                var otherConnections = _activeConnections.Count(x => x.Value != userId);

                _logger.LogInformation(
                    "Broadcasting message - MessageId: {MessageId}, Total connections: {TotalConnections}, Other users: {OtherUsers}",
                    message.MessageId, activeConnections, otherConnections);

                try
                {
                    // Broadcast to all clients except the sender
                    await Clients.Others.SendAsync("ReceiveMessage", message);
                    
                    _logger.LogInformation(
                        "Message broadcast complete - MessageId: {MessageId}, Recipients: {Recipients}",
                        message.MessageId, otherConnections);

                    // Also send a notification
                    await Clients.Others.SendAsync("ReceiveNotification", new
                    {
                        message.MessageId,
                        message.UserId,
                        message.UserName,
                        message.Content,
                        message.Priority,
                        message.Type,
                        message.Timestamp
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to broadcast message - MessageId: {MessageId}, Error: {Error}",
                        message.MessageId, ex.Message);
                    throw new HubException($"Failed to broadcast message: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage: {Error}", ex.Message);
                throw;
            }
        }

        public async Task JoinGroup(string groupName)
        {
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation(
                "Group join attempt - Group: {Group}, UserId: {UserId}, UserName: {UserName}",
                groupName, userId, userName);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation(
                "Group leave - Group: {Group}, UserId: {UserId}, UserName: {UserName}",
                groupName, userId, userName);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
} 