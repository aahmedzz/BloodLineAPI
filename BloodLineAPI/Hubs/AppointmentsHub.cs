using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BloodLineAPI.Hubs
{
    public class AppointmentsHub : Hub
    {
        public async Task JoinCenterGroup(string centerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, centerId);
        }

        public async Task LeaveCenterGroup(string centerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, centerId);
        }
    }
}
