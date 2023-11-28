using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Sectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MF152004.Workerservice.Services
{
    public class SectorServices
    {
        private List<Sector> _sectors = new();
        private readonly ILogger<SectorServices> _logger;

        public SectorServices(ILogger<SectorServices> logger)
        {
            _logger = logger;            
        }

        public void RunService(List<Sector> sectors)
        {
            _sectors = sectors;
            SubscribeEvents();
            _logger.LogInformation("The sectorservices has been started successfully.");
        }

        private void SubscribeEvents()
        {
            if (_sectors != null && _sectors.Any())
            {
                foreach (Sector sector in _sectors)
                    sector.NewPackageInSector += CheckTrackedPackagesInSectors;
            }
            else
                _logger.LogError("No sectors could be subscribed is sectorservices");
        }

        private void CheckTrackedPackagesInSectors(object? sender, TrackedPacket tracking)
        {
            if (tracking is null)
                return;

            if (string.IsNullOrEmpty(tracking.SectorId))
            {
                _logger.LogWarning("Tracked shipments could not be checked, because sector id is empty.");
                return;
            }

            RemoveGhostTrackedPackages(tracking);
        }

        private void RemoveGhostTrackedPackages(TrackedPacket tracking) //remove packages that have not been ejected
        {
            if (tracking.ShipmentId < 1)
            {
                _logger.LogWarning("The remove-ghost-tracked-packages could not be executed. The chipment ID is " +
                    "lesser than 1.");
                return;
            }

            foreach (Sector sector in _sectors)
            {
                if (sector.Id == tracking.SectorId)
                    continue;

                try
                {
                    if (sector.TrackedPacketExists(shipmentId: tracking.ShipmentId))
                    {
                        _logger.LogInformation($"A tracked shipment which is located in sector {tracking.SectorName} " +
                            $"has been detected in sector {sector}. This will be removed.");
                        sector.RemoveTrackedPacket(shipmentId: tracking.ShipmentId);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.ToString());
                }
            }
        }
    }
}
