using BlueApps.MaterialFlow.Common.Connection.Packets;
using MF152004.Models.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.Connection.Packets
{
    public class WeightScanPacket_152004 : ActionPacket
    {
        public Scan? WeightScan{ get; set; }
    }
}
