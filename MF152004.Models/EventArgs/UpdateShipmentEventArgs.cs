using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MF152004.Models.Main;

namespace MF152004.Models.EventArgs
{
    public class UpdateShipmentEventArgs : System.EventArgs
    {
        public List<Shipment>? UpdatedShipments { get; set; }
    }
}
