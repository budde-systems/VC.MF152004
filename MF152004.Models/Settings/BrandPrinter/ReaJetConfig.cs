using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MF152004.Models.Settings.BrandPrinter
{
    public class ReaJetConfig
    {
        public string? Job { get; set; }
        public string? Group { get; set; }
        public string? Object { get; set; }
        public string? Content { get; set; }
        public string? Value { get; set; }
        public string? NoPrintValue { get; set; }
    }
}
