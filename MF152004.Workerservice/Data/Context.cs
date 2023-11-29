using MF152004.Models.Configurations;
using MF152004.Models.Main;

namespace MF152004.Workerservice.Data
{
    public class Context
    {
        public List<Shipment> Shipments { get; set; } = new();
        public List<Scan> WeightScans { get; set; } = new();
        public ServiceConfiguration Config { get; set; } = new();

    }
}
