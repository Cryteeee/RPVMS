using Microsoft.AspNetCore.SignalR;

namespace BlazorApp1.Server.Hubs
{
    public class UserHub : Hub
    {
        public async Task NotifyEmailVerificationStatusChanged(int userId, bool isVerified)
        {
            await Clients.All.SendAsync("EmailVerificationStatusChanged", userId, isVerified);
        }

        public async Task NotifyClientCountChanged(int activeClientCount)
        {
            await Clients.All.SendAsync("ClientCountChanged", activeClientCount);
        }
    }
} 