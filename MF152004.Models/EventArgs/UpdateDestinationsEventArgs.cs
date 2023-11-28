using BlueApps.MaterialFlow.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.EventArgs
{
    public class UpdateDestinationsEventArgs : System.EventArgs
    {
        public List<Destination> UpdatedDestinations { get; set; } = new();
    }
}
