using BlueApps.MaterialFlow.Common.Models;

namespace MF152004.Webservice.Data.PageData
{
    public class ConfigurationDTO
    {
        public List<Country> Countries { get; set; }
        public List<Carrier> Carriers { get; set; }
        public List<ClientReference> ClientReferences { get; set; }
    }
}
