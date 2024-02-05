using MF152004.Models.Configurations;
using MF152004.Models.Main;

namespace MF152004.Workerservice.Data;

public class Context
{
    public List<Shipment> Shipments { get; } = new();
    
    public List<Scan> WeightScans { get; } = new();
    
    public ServiceConfiguration Config { get; set; } = new();
}