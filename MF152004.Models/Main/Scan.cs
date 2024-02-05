using MF152004.Models.Values.Types;
using System.Text.Json.Serialization;

namespace MF152004.Models.Main;

public class Scan
{
    public int Id { get; set; }
    [JsonPropertyName("shipment_id")]
    public int ShipmentId { get; set; }
    [JsonPropertyName("scan_type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ScanType ScanType { get; set; }
    [JsonPropertyName("weight")]
    public double Weight { get; set; }
    public DateTime ScanTime { get; set; }

    public override string ToString() =>
        $"Shipment ID {ShipmentId}, Weight {Weight}";
}