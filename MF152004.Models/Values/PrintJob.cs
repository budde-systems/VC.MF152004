using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.Values
{
    public class PrintJob
    {
        public int ShipmentId { get; set; }
        public string ReferenceId { get; set; }
        public DateTime AtTime { get; set; } = DateTime.Now;
    }
}
