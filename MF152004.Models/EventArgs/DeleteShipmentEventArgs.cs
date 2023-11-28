using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.EventArgs
{
    public class DeleteShipmentEventArgs : System.EventArgs
    {
        public int ShipmentId { get; set; }
    }
}
