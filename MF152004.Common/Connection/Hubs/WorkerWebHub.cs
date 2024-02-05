using MF152004.Models.Connection.Packets.HubPacket;
using Microsoft.AspNetCore.SignalR;

namespace MF152004.Common.Connection.Hubs;

public class WorkerWebHub : Hub
{
    public async Task SendStatus(SystemStatus? status)
    {
        if (status != null)
            await Clients.All.SendAsync("ReceiveStatus", status);
    }

    public async Task SendDestinationStatus(DestinationStatus? destinationStatus)
    {
        if (destinationStatus != null)
            await Clients.All.SendAsync("ReceiveDestinationStatus", destinationStatus);
    }
}