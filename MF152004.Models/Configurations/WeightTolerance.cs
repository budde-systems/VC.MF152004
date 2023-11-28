using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.Configurations
{
    public class WeightTolerance
    {
        public int Id { get; set; }
        public double WeigthTolerance { get; set; }
        public bool ConfigurationInUse { get; set; }
    }
}
