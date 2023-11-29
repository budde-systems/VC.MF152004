using MF152004.Models.Values.Types;

namespace MF152004.Models.Connection.Packets.HubPacket;

public class SystemStatus
{
    public Status CurrentStatus { get; set; }
    public string? Message { get; set; }
    public bool Release { get; set; }
    public string? TransportReference { get; set; }
    public string? ReadedCodes { get; set; }
}