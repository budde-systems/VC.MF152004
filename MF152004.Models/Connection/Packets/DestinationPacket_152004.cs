using BlueApps.MaterialFlow.Common.Connection.Packets;
using BlueApps.MaterialFlow.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.Connection.Packets
{
    public class DestinationPacket_152004 : ActionPacket
    {
        public List<Destination>? Destinations { get; set; }
    }
}
