using System.Text.Json.Serialization;

namespace MF152004.Models.Settings.BrandPrinter;

public class BrandPrinterSettings
{
    public string IPAddress { get; set; }
    public int Port { get; set; }
    public ReaJetConfig Configuration { get; set; }

    [JsonIgnore]
    public string ConnectionString => $"TCP://{IPAddress}:{Port}";
}