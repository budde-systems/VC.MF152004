using MF152004.Models.Values.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MF152004.Models.Main
{
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
}
