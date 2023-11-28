using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MF152004.Models.Configurations
{
    public class SealerRoute
    {
        public int Id { get; set; }
        [JsonPropertyName("box_barcode_reference")]
        public string? BoxBarcodeReference { get; set; }
        [JsonPropertyName("sealer_route_reference")]
        public string? SealerRouteReference { get; set; }
        public bool ConfigurationInUse { get; set; }
    }
}
