namespace MF152004.Models.Settings.BrandPrinter
{
    public class BrandPrinterSettingsBack : IBrandPrinterSettings
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public ReaJetConfig Configuration { get; set; }
    }
}
