using MF152004.Models.Values;

namespace MF152004.Models.EventArgs;

public class FinishedPrintJobEventArgs
{
    public PrintJob Job { get; set; }
    public string BasePositionBrandPrinter { get; set; }
}