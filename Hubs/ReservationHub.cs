using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace PUBReservationSystem.Hubs
{
    public class ReservationHub : Hub
    {
        public async Task SendReservationUpdate(string message, object data)
        {
            await Clients.All.SendAsync("ReceiveReservationUpdate", message, data);
        }

        public async Task NotifyCancellation(string reservationId, string cancelledBy)
        {
            await Clients.All.SendAsync("ReservationCancelled", reservationId, cancelledBy);
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }
}

