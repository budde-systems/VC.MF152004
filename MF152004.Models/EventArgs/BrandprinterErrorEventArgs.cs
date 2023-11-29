namespace MF152004.Models.EventArgs;

public class BrandPrinterErrorEventArgs : System.EventArgs
{
    public string BrandprinterName { get; set; }
    
    public int JobId { get; set; }
    
    public string Message { get; set; }
    
    public short ErrorCode { get; set; }
}