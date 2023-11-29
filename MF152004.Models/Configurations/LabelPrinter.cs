using System.Text.Json.Serialization;

namespace MF152004.Models.Configurations
{
    public class LabelPrinter
    {
        public int Id { get; set; }
        [JsonPropertyName("box_barcode_reference")]
        public string? BoxBarcodeReference { get; set; }
        [JsonPropertyName("label_printer_reference")]
        public string? LabelPrinterReference { get; set; }
        public bool ConfigurationInUse { get; set; }
    }
}
