using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Sectors;

namespace MF152004.Workerservice.Services;

public class DestinationService
{
    private readonly ILogger<DestinationService> _logger;

    private List<Sector>? _sectors;
        

    public DestinationService(ILogger<DestinationService> logger)
    {
        _logger = logger;
    }

    public void SetSectors(List<Sector> sectors) =>
        _sectors = sectors;

    /// <summary>
    /// This will only work if sectors is set
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnDestinationsUpdate(object? sender, Models.EventArgs.UpdateDestinationsEventArgs e)
    {
        var routePositions = GetSectorsRoutePositions()?.ToList();

        if (routePositions != null)
        {
            foreach (var updatedDest in e.UpdatedDestinations)
            {
                var routePosition = routePositions.SingleOrDefault(x => x?.Destination != null && x.Destination.Name == updatedDest.Name);

                routePosition ??= routePositions.SingleOrDefault(x => x?.Destination != null && x.Destination.Id == updatedDest.Id);

                if (routePosition == null)
                    _logger.LogWarning($"Destination {updatedDest.Name} couldn't be found in sectors");
                else
                {
                    routePosition.SetRoutePosition(updatedDest);
                    //destination = updatedDest;
                    _logger.LogInformation($"Destination {updatedDest.Name} has been updated");
                }
            }
        }
    }

    private IEnumerable<RoutePosition>? GetSectorsRoutePositions() =>
        _sectors?
            .Where(x => x.Diverters != null)
            .SelectMany(x => x.Diverters)
            .Where(x => x.Towards != null)
            .SelectMany(x => x.Towards)
            .Where(x => x.RoutePosition != null)
            .Select(x => x.RoutePosition);

    internal IEnumerable<Destination?>? GetSectorsDestinations() =>
        GetSectorsRoutePositions()?
            .Where(p => p is { Destination: not null })
            .Select(p => p.Destination);

    internal void OnDockedTelescope(object? sender, DockedTelescopeEventArgs docked)
    {
        var destinations = GetSectorsDestinations()?.ToList();

        if (destinations != null && docked.Gates != null)
        {
            DeactivateAllGates(destinations);

            foreach (var gate in docked.Gates)
            {
                var destination = destinations.FirstOrDefault(d => d is { UI_Id: not null } && string.Concat(d.UI_Id.Where(char.IsDigit)) == gate);

                if (destination != null)
                {
                    destination.Active = true;
                    _logger.LogInformation($"Destination {destination.Name} has been activated");
                }
            }
        }
    }

    private void DeactivateAllGates(IEnumerable<Destination?>? destinations) =>
        destinations?.ToList().ForEach(_ => { if (_ != null && _.Name != "Tor 1" && _.Name != "Tor 2" && _.Name != "Tor 3") _.Active = false; }); //TODO: Anpassen!

    internal void OnLoadFactor(object? sender, LoadFactorEventArgs e)
    {
        var destinations = GetSectorsDestinations()?.ToList();

        if (destinations != null && e.LoadFactors != null)
        {
            foreach (var loadFactor in e.LoadFactors)
            {
                var destination = destinations.FirstOrDefault(_ => _ is { UI_Id: not null } && string.Concat(_.UI_Id.Where(char.IsDigit)) == loadFactor.Gate);

                if (destination != null)
                    destination.LoadFactor = loadFactor.Factor; 
            }
        }
    }
}