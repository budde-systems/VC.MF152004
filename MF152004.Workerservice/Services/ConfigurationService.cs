using BlueApps.MaterialFlow.Common.Models.Configurations;
using MF152004.Models.Configurations;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Workerservice.Services
{
    public class ConfigurationService
    {
        private readonly Context _context; //TODO: lock ergänzen

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConfigurationService(Context context)
        {
            if (context == null) 
                throw new ArgumentNullException("context");

            _context = context;
        }

        public void UpdateCongigs(ServiceConfiguration? configuration)
        {
            if (configuration != null)
            {
                _context.Config = configuration;

                CommonData.WeightTolerance = configuration.WeightToleranceConfig.WeigthTolerance;
            } //TODO: else logging error
        }

        public bool ConfigHasEntities()
        {
            return _context.Config != null && _context.Config.SealerRouteConfigs.Count > 0;
        }
    }
}
