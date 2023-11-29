using BlueApps.MaterialFlow.Common.Connection.Packets;
using MF152004.Models.Main;

namespace MF152004.Models.Connection.Packets;

public class ShipmentPacket_152004 : ActionPacket //Das wird über MessagePacket durch PacketHelper versendet.
{
    public List<Shipment>? Shipments { get; set; }
    public List<int>? RequestedShipments { get; set; }
}