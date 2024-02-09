using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models.Types;
using BlueApps.MaterialFlow.Common.Models;
using MF152004.Models.Values.Types;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Services;
using BlueApps.MaterialFlow.Common.Sectors;

namespace MF152004.Workerservice.Sectors.Gates;

public class TelescopeGatesSectorA : GatesSector
{
    private const string NAME = "Telescope Sector 1";

    public TelescopeGatesSectorA(MqttClient client, ILogger<Sector> logger, string basePosition, ContextService contextService, 
        MessageDistributor messageDistributor) : base(client, logger, basePosition, NAME, contextService, messageDistributor)
    {
        BarcodeScanner = CreateScanner();
        Diverters = CreateDiverters();
    }

    public override ICollection<IDiverter> CreateDiverters()
    {
        return new List<IDiverter> { CreateFlowSort_1(), CreateFlowSort_2(), CreateFlowSort_3() };
    }

    private FlowSort CreateFlowSort_1()
    {
        FlowSort flowSort = new()
        {
            Name = NAME + " flowsort 1",
            BasePosition = "6.1.130",
            SubPosition = "6.1.129"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = "Tor 4" });
        
        flowSort.CreateTowards(new Toward
        {
            DriveDirection = Direction.Left,
            RoutePosition = routePosition
        }, new Toward
        {
            DriveDirection = Direction.StraightAhead,
            FaultDirection = true,
            RoutePosition = new RoutePosition
            {
                Id = "1",
                Name = DefaultRoute.ToGates.ToString()
            }
        });

        flowSort.SetRelatedScanner(BarcodeScanner);

        return flowSort;
    }

    private FlowSort CreateFlowSort_2()
    {
        FlowSort flowSort = new()
        {
            Name = NAME + " flowsort 2",
            BasePosition = "6.2.137",
            SubPosition = "6.2.136"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = "Tor 5" });
        
        flowSort.CreateTowards(new Toward
        {
            DriveDirection = Direction.Left,
            RoutePosition = routePosition
        }, new Toward
        {
            DriveDirection = Direction.StraightAhead,
            FaultDirection = true,
            RoutePosition = new RoutePosition
            {
                Id = "2",
                Name = DefaultRoute.ToGates.ToString()
            }
        });

        flowSort.SetRelatedScanner(BarcodeScanner);

        return flowSort;
    }

    private FlowSort CreateFlowSort_3()
    {
        FlowSort flowSort = new()
        {
            Name = NAME + " flowsort 3",
            BasePosition = "7.1.144",
            SubPosition = "7.1.143"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = "Tor 6" });
        
        flowSort.CreateTowards(new Toward
        {
            DriveDirection = Direction.Left,
            RoutePosition = routePosition
        }, new Toward
        {
            DriveDirection = Direction.StraightAhead,
            FaultDirection = true,
            RoutePosition = new RoutePosition
            {
                Id = "3",
                Name = DefaultRoute.ToGates.ToString()
            }
        });

        flowSort.SetRelatedScanner(BarcodeScanner);

        return flowSort;
    }

    public override Scanner CreateScanner() => new("M6.1.205", "S6.1.206");
}