using Microsoft.AspNetCore.SignalR;

namespace RestaurantSystem.Api.Hubs;

public class OrderHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }
}
