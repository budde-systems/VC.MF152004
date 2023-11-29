using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models.Types;
using BlueApps.MaterialFlow.Common.Models;
using MF152004.Models.Values.Types;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Services;

namespace MF152004.Workerservice.Sectors.Gates;

public class TelescopeGatesSectorB : GatesSector
{
    private const string NAME = "Telescope Sector 2";

    public TelescopeGatesSectorB(IClient client, string basePosition, ContextService contextService, 
        MessageDistributor messageDistributor) : base(client, basePosition, NAME, contextService, messageDistributor)
    {
        BarcodeScanner = CreateScanner();
        Diverters = CreateDiverters();
    }

    public override ICollection<IDiverter> CreateDiverters()
    {
        var flowsort1 = CreateFlowSort_1();
        var flowsort2 = CreateFlowSort_2();
        var flowsort3 = CreateFlowSort_3();
        var flowsort4 = CreateFlowSort_4();

        return new List<IDiverter> { flowsort1, flowsort2, flowsort3, flowsort4 };
    }

    private FlowSort CreateFlowSort_1()
    {
        FlowSort flowSort = new()
        {
            Name = NAME + " flowsort 1",
            BasePosition = "7.2.154",
            SubPosition = "7.2.153"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = "Tor 7" });
        
        flowSort.CreateTowards(
            new Toward
            {
                DriveDirection = Direction.Left,
                RoutePosition = routePosition,
            },
            new Toward
            {
                DriveDirection = Direction.StraightAhead,
                FaultDirection = true,
                RoutePosition = new RoutePosition
                {
                    Id = "1",
                    Name = DefaultRoute.ToGates.ToString(),
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
            BasePosition = "7.2.158",
            SubPosition = "7.2.157"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = "Tor 8" });

        flowSort.CreateTowards(
            new Toward
            {
                DriveDirection = Direction.Left,
                RoutePosition = routePosition,
            }, 
            new Toward
            {
                DriveDirection = Direction.StraightAhead,
                FaultDirection = true,
                RoutePosition = new RoutePosition
                {
                    Id = "2",
                    Name = DefaultRoute.ToGates.ToString(),
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
            BasePosition = "8.1.165",
            SubPosition = "8.1.164"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = "Tor 9" });

        flowSort.CreateTowards(
            new Toward
            {
                DriveDirection = Direction.Left,
                RoutePosition = routePosition,
            },
            new Toward
            {
                DriveDirection = Direction.StraightAhead,
                FaultDirection = true,
                RoutePosition = new RoutePosition
                {
                    Id = "3",
                    Name = DefaultRoute.ToGates.ToString(),
                }
            });

        flowSort.SetRelatedScanner(BarcodeScanner);

        return flowSort;
    }

    private FlowSort CreateFlowSort_4()
    {
        FlowSort flowSort = new()
        {
            Name = NAME + " flowsort 4",
            BasePosition = "8.2.175",
            SubPosition = "8.2.174"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = "Tor 10" });

        flowSort.CreateTowards(
            new Toward
            {
                DriveDirection = Direction.Left,
                RoutePosition = routePosition,
            },
            new Toward
            {
                DriveDirection = Direction.StraightAhead,
                FaultDirection = true,
                RoutePosition = new RoutePosition
                {
                    Id = "4",
                    Name = DefaultRoute.ToGates.ToString(),
                }
            });

        flowSort.SetRelatedScanner(BarcodeScanner);

        return flowSort;
    }

    public override Scanner CreateScanner() => new("M7.1.208", "S7.1.209");
}