using Microsoft.AspNetCore.SignalR;

namespace parkmeter_management.Infrastructure.SignalR
{
    public class ParkMeterHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine(
                $"Client connected: {Context.ConnectionId}");
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