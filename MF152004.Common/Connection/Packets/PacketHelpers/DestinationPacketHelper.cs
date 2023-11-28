using BlueApps.MaterialFlow.Common.Connection.Packets;
using BlueApps.MaterialFlow.Common.Connection.PackteHelper;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Models.Connection.Packets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MF152004.Common.Connection.Packets.PacketHelpers
{
    public class DestinationPacketHelper : MessagePacketHelper
    {
        public override string InTopic { get; set; }
        public override string OutTopic { get; set; }

        public DestinationPacket_152004? DestinationPacket { get; set; }

        public DestinationPacketHelper(string inTopic, string outTopic)
        {
            InTopic = inTopic;
            OutTopic = outTopic;
        }

        public override MessagePacket GetPacketData()
        {
            MessagePacket packet = new()
            {
                Topic = OutTopic,
                Data = JsonSerializer.Serialize(DestinationPacket)
            };

            return packet;
        }

        public override void SetPacketData(MessagePacket message)
        {
            if (message != null && !string.IsNullOrEmpty(message.Data))
            {
                DestinationPacket = JsonSerializer.Deserialize<DestinationPacket_152004>(message.Data) ?? new();
            }
        }

        public void CreateDestinationRequest() =>
            DestinationPacket = new() { KeyCode = ActionKey.RequestedEntity };

        public void CreateDestinationResponse(params Destination[] destinations)
        {
            DestinationPacket = new()
            {
                KeyCode = ActionKey.UpdatedEntity,
                Destinations = destinations.ToList()
            };
        }
    }
}
