using BlueApps.MaterialFlow.Common.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MF152004.Models.Main
{
    public class Shipment : IShipment
    {
        [JsonPropertyName("shipment_id")]
        public int Id { get; set; }
        [JsonPropertyName("client_reference")]
        public string? ClientReference { get; set; }
        [JsonPropertyName("box_barcode_reference")]
        public string? BoxBarcodeReference { get; set; }
        [JsonPropertyName("transportation_reference")]
        public string? TransportationReference { get; set; }
        [JsonPropertyName("tracking_code")]
        public string? TrackingCode { get; set; }
        [JsonPropertyName("carrier")]
        public string? Carrier { get; set; }
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        [JsonPropertyName("status")]
        public string? Status { get; set; } //TODO: Wenn pending, Prozessbeschr. beachten
        [JsonPropertyName("weight")]
        public double Weight { get; set; }
        [JsonPropertyName("conveyor_belt_message")]
        public string? Message { get; set; }
        [JsonPropertyName("box_branded_at_1")]
        public DateTime? BoxBrandedAt_1 { get; set; }
        [JsonPropertyName("box_branded_at_2")]
        public DateTime? BoxBrandedAt_2 { get; set; }
        [JsonPropertyName("label_printed_at")]
        public DateTime? LabelPrintedAt { get; set; }
        [JsonPropertyName("label_printing_failed_at")]
        public DateTime? LabelPrintingFailedAt { get; set; }
        [JsonPropertyName("left_sealer_at")]
        public DateTime? LeftSealerAt { get; set; }
        [JsonPropertyName("destination_route_reference")]
        public string? DestinationRouteReference { get; set; }
        [JsonPropertyName("destination_route_reference_updated_at")]
        public DateTime? DestinationRouteReferenceUpdatedAt { get; set; }
        [JsonPropertyName("received_at")]
        public DateTime? ReceivedAt { get; set; }
        /// <summary>
        /// Tracing packet on the conveyor system
        /// </summary>
        [NotMapped]
        public int PacketTracing { get; set; }
        [JsonPropertyName("left_error_aisle_at")]
        public DateTime? LeftErrorAisleAt { get; set; }
        [JsonPropertyName("destination_reached_at")]
        public DateTime? DestinationReachedAt { get; set; }

        public override string ToString() =>
            $"ID {Id}{(!string.IsNullOrEmpty(TransportationReference) ? $" - {TransportationReference}" : "")}";
    }
}