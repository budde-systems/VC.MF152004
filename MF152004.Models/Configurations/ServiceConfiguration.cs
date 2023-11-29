namespace MF152004.Models.Configurations;

public class ServiceConfiguration
{
    public WeightTolerance? WeightToleranceConfig { get; set; } = new();
    public List<BrandingPdf> BrandingPdfConfigs { get; set; } = new();
    public List<LabelPrinter> LablePrinterConfigs { get; set; } = new();
    public List<SealerRoute> SealerRouteConfigs { get; set; } = new();
}