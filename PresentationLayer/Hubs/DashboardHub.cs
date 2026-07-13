using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace PresentationLayer.Hubs
{
    [Authorize(Roles = "admin")]
    public class DashboardHub : Hub
    {
        /// <summary>
        /// Admin clients can join a specific dashboard group to receive real-time updates.
        /// </summary>
        public async Task JoinDashboard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminDashboard");
        }

        /// <summary>
        /// Admin clients leave the dashboard group.
        /// </summary>
        public async Task LeaveDashboard()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminDashboard");
        }
    }
}
