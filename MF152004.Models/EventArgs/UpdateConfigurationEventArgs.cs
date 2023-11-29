using MF152004.Models.Configurations;

namespace MF152004.Models.EventArgs
{
    public class UpdateConfigurationEventArgs : System.EventArgs
    {
        public ServiceConfiguration? ServiceConfiguration { get; set; }
    }
}
