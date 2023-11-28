using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Machines;
using MF152004.Workerservice.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MF152004.Workerservice.Connection.Packets;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.Types;
using MF152004.Models.Values.Types;

namespace MF152004.Workerservice.Sectors.Gates
{
    public class ExportGates : GatesSector //TODO: Gates können alle zusammengefasst werden. Scanner => Scanners : List<T>
    {
        private const string NAME = "Export Sector";
        public ExportGates(IClient client, string baseposition, ContextService contextService,
            MessageDistributor messageDistributor) : base(client, baseposition, NAME, contextService, messageDistributor)
        {
            BarcodeScanner = CreateScanner();
            Diverters = CreateDiverters();
        }

        public override ICollection<IDiverter> CreateDiverters() //TODO: 3 diverters
        {
            var flowsort1 = CreateFlowSort_1();
            var flowsort2 = CreateFlowSort_2();
            var flowsort3 = CreateFlowSort_3();

            return new List<IDiverter> { flowsort1, flowsort2, flowsort3 };
        }

        private FlowSort CreateFlowSort_1()
        {
            FlowSort flowSort = new()
            {
                Name = NAME + " flowsort 1",
                BasePosition = "5.1.107",
                SubPosition = "5.1.108"
            };

            RoutePosition routePosition = new();
            routePosition.SetRoutePosition(new Destination { Name = "Tor 1", Active = true }); //TODO: UI_Id verwebden
            flowSort.CreateTowards(new[]
            {
                new Toward()
                {
                    DriveDirection = Direction.Left,
                    RoutePosition = routePosition,
                },

                new Toward()
                {
                    DriveDirection = Direction.StraightAhead,
                    FaultDirection = true,
                    RoutePosition = new RoutePosition()
                    {
                        Id = "1",
                        Name = DefaultRoute.ToGates.ToString(),
                    }
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
                BasePosition = "5.2.113",
                SubPosition = "5.2.112"
            };

            RoutePosition routePosition = new();
            routePosition.SetRoutePosition(new Destination { Name = "Tor 2", Active = true });
            flowSort.CreateTowards(new[]
            {
                new Toward()
                {
                    DriveDirection = Direction.Left,
                    RoutePosition = routePosition,
                },

                new Toward()
                {
                    DriveDirection = Direction.StraightAhead,
                    FaultDirection = true,
                    RoutePosition = new RoutePosition()
                    {
                        Id = "2",
                        Name = DefaultRoute.ToGates.ToString(),
                    }
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
                BasePosition = "5.2.119",
                SubPosition = "5.2.118"
            };

            RoutePosition routePosition = new();
            routePosition.SetRoutePosition(new Destination { Name = "Tor 3", Active = true });
            flowSort.CreateTowards(new[]
            {
                new Toward()
                {
                    DriveDirection = Direction.Left,
                    RoutePosition = routePosition,
                },

                new Toward()
                {
                    DriveDirection = Direction.StraightAhead,
                    FaultDirection = true,
                    RoutePosition = new RoutePosition()
                    {
                        Id = "3",
                        Name = DefaultRoute.ToGates.ToString(),
                    }
                }
            });

            flowSort.SetRelatedScanner(BarcodeScanner);

            return flowSort;
        }

        public override Scanner CreateScanner() =>
            new("M5.1.202", "S5.1.203");
    }
}
