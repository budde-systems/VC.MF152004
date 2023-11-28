using MF152004.Models.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Models.EventArgs
{
    public class UpdateConfigurationEventArgs : System.EventArgs
    {
        public ServiceConfiguration? ServiceConfiguration { get; set; }
    }
}
