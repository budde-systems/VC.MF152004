using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MF152004.Models.Settings.BrandPrinter
{
    public interface IBrandPrinterSettings
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public ReaJetConfig Configuration { get; set; }
    }
}
