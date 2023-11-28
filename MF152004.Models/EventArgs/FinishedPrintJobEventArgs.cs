using MF152004.Models.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.EventArgs
{
    public class FinishedPrintJobEventArgs
    {
        public PrintJob Job { get; set; }
        public string BasePositionBrandPrinter { get; set; }
    }
}
