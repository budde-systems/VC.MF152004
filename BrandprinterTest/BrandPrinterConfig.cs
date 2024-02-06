namespace BrandprinterTest;

public class BrandPrinterConfig
{
    public string ConnectionString { get; set; }

    public string? JobFile { get; set; }
    public string? Group { get; set; }
    public string? Object { get; set; }
    public string? Content { get; set; }
    public string? Value { get; set; }
    public string? NoPrintValue { get; set; }
}