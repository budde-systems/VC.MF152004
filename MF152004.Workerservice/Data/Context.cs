using BlueApps.MaterialFlow.Common.Models.Configurations;
using MF152004.Models.Configurations;
using MF152004.Models.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Workerservice.Data
{
    public class Context
    {
        public List<Shipment> Shipments { get; set; } = new();
        public List<Scan> WeightScans { get; set; } = new();
        public ServiceConfiguration Config { get; set; } = new();

    }
}
