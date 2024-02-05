using BlueApps.MaterialFlow.Common.Connection.Packets;
using BlueApps.MaterialFlow.Common.Models;

namespace MF152004.Models.Connection.Packets;

public class DestinationPacket_152004 : ActionPacket
{
    public List<Destination>? Destinations { get; set; }
}