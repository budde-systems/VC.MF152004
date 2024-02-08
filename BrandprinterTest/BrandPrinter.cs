namespace BrandprinterTest;

public class BrandPrinter
{
    public BrandPrinterConfig Settings { get; } = new();

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string? Name { get; set; }
    
    public string BasePosition { get; set; }
    
    public string SubPosition { get; set; }

    public override string ToString() => Name ?? "BrandPrinter";
}