using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace parkmeter_management.Infrastructure.SignalR
{
    [Authorize]
    public class ParkMeterHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (role != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
                Console.WriteLine($"[HUB] {role} connected → joined group {role}");
            }

            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
           
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            Console.WriteLine(
                $"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}