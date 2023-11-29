namespace MF152004.Models.EventArgs
{
    public class BrandprinterErrorEventArgs : System.EventArgs
    {
        public string BrandprinterName { get; set; }
        public int JobId { get; set; }
        public string Message { get; set; }
        public short Errorcode { get; set; }
    }
}
