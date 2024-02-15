namespace MF152004.Models.Settings.BrandPrinter;

public class ReaJetConfig
{
    public string? Job { get; set; }
    
    public string? Group { get; set; }
    
    public string? Object { get; set; }
    
    public string? Content { get; set; }
    
    public string NoPrintValue { get; set; } = "1";
    
    public int ResetTimeout { get; set; } = 10000;
}