using BlueApps.MaterialFlow.Common.Connection.Packets;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Models.Configurations;
using MF152004.Models.Connection.Packets;
using System.Text.Json;
using BlueApps.MaterialFlow.Common.Connection.PacketHelper;

namespace MF152004.Common.Connection.Packets.PacketHelpers;

public class ConfigurationPacketHelper : MessagePacketHelper
{
    public override string InTopic { get; set; }
    public override string OutTopic { get; set; } //nothing to send at this time
    public ConfigPacket_152004 ConfigurationPacket { get; set; }

    public ConfigurationPacketHelper(string inTopic, string outTopic)
    {
        InTopic = inTopic;
        OutTopic = outTopic;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override MessagePacket GetPacketData()
    {
        if (ConfigurationPacket == null) 
            throw new InvalidOperationException();

        var packet = new MessagePacket
        {
            Topic = OutTopic,
            Data = JsonSerializer.Serialize(ConfigurationPacket)
        };

        return packet;
    }

    public override void SetPacketData(MessagePacket message)
    {
        if (message != null && !string.IsNullOrEmpty(message.Data))
        {
#pragma warning disable CS8601 // Mögliche Nullverweiszuweisung.
            ConfigurationPacket = JsonSerializer.Deserialize<ConfigPacket_152004>(message.Data);
#pragma warning restore CS8601 // Mögliche Nullverweiszuweisung.
        }
    }

    public void CreateNewConfigurationRequest() =>
        ConfigurationPacket = new ConfigPacket_152004 { KeyCode = ActionKey.RequestedEntity };

    public void CreateNewConfigurationResponse(ServiceConfiguration newConfiguration) //TODO: ggf. in der abstrakten Klasse aufnehmen als SetNewEntity<T>(T entity){...}
    {
        ConfigurationPacket = new()
        {
            KeyCode = ActionKey.NewEntity,
            Configuration = newConfiguration
        };
    }

    public void CreateUpdatedConfigurationResponse(ServiceConfiguration updatedConfiguration)
    {
        ConfigurationPacket = new()
        {
            KeyCode = ActionKey.UpdatedEntity,
            Configuration = updatedConfiguration
        };
    }
}