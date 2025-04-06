using Microsoft.AspNetCore.SignalR;
using BlazorApp1.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BlazorApp1.Server.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var user = Context.User;
                _logger.LogInformation("Connection attempt. ConnectionId: {ConnectionId}, IsAuthenticated: {IsAuthenticated}", 
                    Context.ConnectionId, user?.Identity?.IsAuthenticated);

                if (user?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("User connected to NotificationHub. ConnectionId: {ConnectionId}", Context.ConnectionId);

                    // Log user claims
                    foreach (var claim in user.Claims)
                    {
                        _logger.LogInformation("User claim - Type: {ClaimType}, Value: {ClaimValue}", claim.Type, claim.Value);
                    }

                    var roles = user.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();

                    _logger.LogInformation("User roles: {Roles}", string.Join(", ", roles));

                    // Add to admin group if user is admin or superadmin
                    if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                        _logger.LogInformation("Added user to Admins group. ConnectionId: {ConnectionId}", Context.ConnectionId);
                    }
                    else
                    {
                        _logger.LogInformation("User not added to Admins group (not an admin). Roles: {Roles}", string.Join(", ", roles));
                    }
                }
                else
                {
                    _logger.LogWarning("Unauthenticated user attempted to connect. ConnectionId: {ConnectionId}", Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var user = Context.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("User disconnected from NotificationHub. ConnectionId: {ConnectionId}", Context.ConnectionId);

                    var roles = user.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();

                    // Remove from admin group if user was admin or superadmin
                    if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
                    {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
                        _logger.LogInformation("Removed user from Admins group. ConnectionId: {ConnectionId}", Context.ConnectionId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinAdminGroup()
        {
            try
            {
                var user = Context.User;
                if (user?.Identity?.IsAuthenticated == true && (user.IsInRole("Admin") || user.IsInRole("SuperAdmin")))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                    _logger.LogInformation("User manually joined Admins group. ConnectionId: {ConnectionId}", Context.ConnectionId);
                }
                else
                {
                    _logger.LogWarning("Unauthorized attempt to join Admins group. ConnectionId: {ConnectionId}", Context.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinAdminGroup");
            }
        }

        public async Task SendNotification(BoardMessageDto message)
        {
            try
            {
                _logger.LogInformation("Sending notification. Type: {Type}, MessageId: {MessageId}, Content: {Content}", 
                    message.Type, message.MessageId, message.Content);

                if (message.Type == "UserRegistration")
                {
                    _logger.LogInformation("Sending UserRegistration notification to Admins group");
                    await Clients.Group("Admins").SendAsync("ReceiveNotification", message);
                    _logger.LogInformation("Successfully sent UserRegistration notification to Admins group");
                }
                else
                {
                    _logger.LogInformation("Sending notification to all clients");
                    await Clients.All.SendAsync("ReceiveNotification", message);
                    _logger.LogInformation("Successfully sent notification to all clients");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification. Type: {Type}, MessageId: {MessageId}", 
                    message.Type, message.MessageId);
            }
        }
    }
} 