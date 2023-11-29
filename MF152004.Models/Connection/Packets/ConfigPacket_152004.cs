using BlueApps.MaterialFlow.Common.Connection.Packets;
using MF152004.Models.Configurations;

namespace MF152004.Models.Connection.Packets;

public class ConfigPacket_152004 : ActionPacket
{
    public ServiceConfiguration Configuration { get; set; }
}