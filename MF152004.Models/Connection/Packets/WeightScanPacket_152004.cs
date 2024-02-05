using BlueApps.MaterialFlow.Common.Connection.Packets;
using MF152004.Models.Main;

namespace MF152004.Models.Connection.Packets;

public class WeightScanPacket_152004 : ActionPacket
{
    public Scan? WeightScan{ get; set; }
}