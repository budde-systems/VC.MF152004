using BlueApps.MaterialFlow.Common.Models.Configurations;
using MF152004.Models.Values.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MF152004.Models.Configurations
{
    public class ServiceConfiguration
    {
        public WeightTolerance? WeightToleranceConfig { get; set; } = new();
        public List<BrandingPdf> BrandingPdfConfigs { get; set; } = new();
        public List<LabelPrinter> LablePrinterConfigs { get; set; } = new();
        public List<SealerRoute> SealerRouteConfigs { get; set; } = new();
    }
}
