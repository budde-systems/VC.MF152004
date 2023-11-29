using System.Text.Json.Serialization;

namespace MF152004.Models.Configurations;

public class BrandingPdf
{
    public int Id { get; set; }
    [JsonPropertyName("box_barcode_reference")]
    public string? BoxBarcodeReference { get; set; }
    [JsonPropertyName("client_reference")]
    public string? ClientReference { get; set; }
    [JsonPropertyName("branding_pdf_reference")]
    public string? BrandingPdfReference { get; set; }
    public bool ConfigurationInUse { get; set; }
}