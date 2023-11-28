using BlueApps.MaterialFlow.Common.Connection.PacketHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Workerservice.Connection.Packets.Settings
{
    public class PacketSettings : IPacketSettings
    {
        public int[] AreaLengths { get; set; } = new int[] { 4, 5, 3, 5, 10, 20, 30, 30, 30, 30, 30, 30 };
        public int MaxStringLength { get; set; } = 238;
        public int MaxAreaLength { get; set; } = 12;
        public char Delimeter { get; set; } = ';';
        public char FillChar { get; set; } = '#';
    }
}
