using BlueApps.MaterialFlow.Common.Connection.Packets;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Models.Connection.Packets;
using MF152004.Models.Main;
using System.Text.Json;
using BlueApps.MaterialFlow.Common.Connection.PacketHelper;

namespace MF152004.Common.Connection.Packets.PacketHelpers;

public class WeightScanMessagePacketHelper : MessagePacketHelper
{
    public override string InTopic { get; set; }

    public override string OutTopic { get; set; }

    public WeightScanPacket_152004? WeightScanPacket { get; set; }

    public WeightScanMessagePacketHelper(string inTopic, string outTopic)
    {
        InTopic = inTopic;
        OutTopic = outTopic;
    }

    public override MessagePacket GetPacketData()
    {
        var packet = new MessagePacket
        {
            Topic = OutTopic,
            Data = JsonSerializer.Serialize(WeightScanPacket)
        };

        return packet;
    }

    public override void SetPacketData(MessagePacket message)
    {
        if (message != null && !string.IsNullOrEmpty(message.Data))
        {
            WeightScanPacket = JsonSerializer.Deserialize<WeightScanPacket_152004>(message.Data);
        }
    }

    public void CreateNewWeightScanResponse(Scan scan)
    {
        WeightScanPacket = new()
        {
            KeyCode = ActionKey.NewEntity,
            WeightScan = scan
        };
    }
}