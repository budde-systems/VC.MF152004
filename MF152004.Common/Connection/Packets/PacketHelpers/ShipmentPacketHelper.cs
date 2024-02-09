using BlueApps.MaterialFlow.Common.Connection.Packets;
using BlueApps.MaterialFlow.Common.Connection.PackteHelper;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Models.Connection.Packets;
using MF152004.Models.Main;
using System.Text.Json;

namespace MF152004.Common.Connection.Packets.PacketHelpers;

public class ShipmentPacketHelper : MessagePacketHelper
{
    public override string InTopic { get; set; }
    public override string OutTopic { get; set; }
        
    public ShipmentPacket_152004 ShipmentPacket { get; set; }

    public ShipmentPacketHelper(string inTopic, string outTopic)
    {
        InTopic = inTopic;
        OutTopic = outTopic;
    }

    public void CreateShipmentsPacket(ActionKey keyCode, params Shipment[] shipments)
    {
        ShipmentPacket = new ShipmentPacket_152004
        {
            KeyCode = keyCode,
            Shipments = shipments.ToList()
        };
    }

    public override MessagePacket GetPacketData()
    {
        var packet = new MessagePacket
        { 
            Topic = OutTopic,
            Data = JsonSerializer.Serialize(ShipmentPacket)
        };

        return packet;
    }

    public override void SetPacketData(MessagePacket message)
    {
        if (message != null && !string.IsNullOrEmpty(message.Data))
        {
#pragma warning disable CS8601 // Mögliche Nullverweiszuweisung.
            ShipmentPacket = JsonSerializer.Deserialize<ShipmentPacket_152004>(message.Data);
#pragma warning restore CS8601 // Mögliche Nullverweiszuweisung.
        }
    }

    public void CreateNewShipmentsRequest(params int[] requestedShipments) =>
        ShipmentPacket = new ShipmentPacket_152004 
        { 
            KeyCode = ActionKey.RequestedEntity,
            RequestedShipments = requestedShipments?.ToList() 
        };

    public void CreateNewShipmentsRespose(params Shipment[] shipments)
    {
        ShipmentPacket = new()
        {
            KeyCode = ActionKey.NewEntity,
            Shipments = shipments.ToList()
        };
    }

    public void CreateUpdatedShipmentsResponse(params Shipment[] shipments)
    {
        ShipmentPacket = new()
        {
            KeyCode = ActionKey.UpdatedEntity,
            Shipments = shipments.ToList()
        };
    }

}