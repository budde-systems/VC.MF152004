using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Models.Types;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Models.Values.Types;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Services;

namespace MF152004.Workerservice.Sectors;

public class BoxSealerSector : Sector
{
    private const string NAME = "BoxSealer";

    private readonly ContextService _contextService;
    private readonly PLC152004_PacketHelper _packetHelper = new();
    private readonly MessageDistributor _messageDistributor;

    public BoxSealerSector(MqttClient client, ILogger<Sector> logger, string baseposition, ContextService contextService, 
        MessageDistributor messageDistributor) : base(client, logger, NAME, baseposition)
    {
        _contextService = contextService;
        _messageDistributor = messageDistributor;
        AddRelatedErrorCodes();
        BarcodeScanner = CreateScanner();
        Diverters = CreateDiverters();            
    }

    public override void AddRelatedErrorCodes()
    {
        var errors = new List<Errorcode>
        {
            Errorcode.EmergencyHold_Boxsealer //TODO: weitere ergänzen
        };

        RelatedErrorcodes.AddRange(errors.Cast<short>());
    }

    public override List<IDiverter> CreateDiverters()
    {
        FlowSort flowSort = new()
        {
            Name = NAME,
            BasePosition = "2.2.52",
            SubPosition = "2.2.53"
        };

        flowSort.CreateTowards(new Toward
        {
            DriveDirection = Direction.Right,
            FaultDirection = true,
            RoutePosition = new RoutePosition
            {
                Id = "1", //1 = detour
                Name = DefaultRoute.Detour.ToString(),
            }
        }, new Toward
        {
            DriveDirection = Direction.StraightAhead,
            RoutePosition = new RoutePosition
            {
                Id = "2", //2 = straight out
                Name = DefaultRoute.BoxSealer.ToString()
            }
        });

        flowSort.SetRelatedScanner(BarcodeScanner);

        List<IDiverter> diverters = new() { flowSort };

        return diverters;
    }

    public override Scanner CreateScanner() => new("M2.1.187", "S2.1.187");

    public override void Barcode_Scanned(object? sender, BarcodeScanEventArgs scan)
    {
        if (BarcodeScanner.BasePosition == scan.Position)
        {
            var diverter = Diverters.FirstOrDefault(div => div.DivertersScanner(scan.Position));

            try
            {
                var shipment = _contextService.LogShipment(this, scan.Barcodes);

                if (shipment != null && scan.Barcodes?.Any(_ => _ == CommonData.NoRead) == true)
                {
                    _logger.LogInformation("{0}: NO_READ barcode detected: {1}", this, shipment);
                    shipment = null;
                }

                var shipmentId = shipment?.Id ?? -1;

                if (shipmentId > 0)
                {
                    _contextService.SetPacketTracing(scan.PacketTracing, scan.Barcodes?.ToArray() ?? new[] { "" });
                    AddTrackedPacket(scan.PacketTracing, shipmentId);
                }

                SetDiverterDirection(diverter, shipmentId, scan.Barcodes?.ToArray());

                _packetHelper.Create_FlowSortPosition(diverter, scan.PacketTracing);

                _client.SendData(_packetHelper.GetPacketData()); //TODO: VK: This is dangerous, needs to be reworked to so async call gets awaited
                _logger.LogInformation("{0}: Data has been send to plc", this);

                ShipmentErrorHandling(shipmentId);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
            }

            OnFaultyBarcodes(scan);
        }
    }

    private void OnFaultyBarcodes(BarcodeScanEventArgs scan)
    {
        if (scan.Barcodes is null)
        {
            //TODO: Logging
        }
        else if (scan.Barcodes.Contains(CommonData.NoRead))
        {
            NoRead noRead = new()
            {
                AtTime = scan.AtTime,
                Position = scan.Position ?? "No Position",
            };

            _messageDistributor.SendNoRead(noRead);
        }
    }

    private void SetDiverterDirection(IDiverter? diverter, int shipmentId, params string[]? barcodes) //TODO: Logging
    {
        if (diverter is null)
            throw new ArgumentNullException(nameof(diverter));

        if (shipmentId <= 0)
        {
            diverter.SetFaultDirection();
            _logger.LogWarning($"Invalid shipment ID in sector {this}. " +
                               $"Received barcodes: {(barcodes != null ? string.Join(", ", barcodes) : "null")}");
            return;
        }

        if (!_contextService.IsShipped(shipmentId))
        {
            diverter.SetFaultDirection();
            _contextService.SetMessage("1000The package has the wrong status", shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);
            _logger.LogWarning($"Shipment ID {shipmentId} has the wrong status in sector {this}");
            return;
        }

        var sealerReference = _contextService.GetBoxSealerRoute(barcodes);

        if (string.IsNullOrEmpty(sealerReference))
        {
            diverter.SetFaultDirection();
            _contextService.SetMessage("1001The sealer-route-reference could not be allocated", shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);
            _logger.LogWarning($"The sealer route could not be allocated for shipment ID {shipmentId} in sector {this}");
            return;
        }

        var dir = diverter.Towards.FirstOrDefault(x => x.RoutePosition.Id == sealerReference)?.DriveDirection;

        if (dir is null)
        {
            diverter.SetFaultDirection();
            _contextService.SetMessage("1002Internal error: the direction of the diverter could not be set.", shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);
            _logger.LogWarning($"The direction of the diverter could not be set for shipment ID {shipmentId} in sector {this}");
        }
        else
            diverter.SetDirection((Direction)dir);
    }

    private void ShipmentErrorHandling(int shipmentId)
    {
        var shipment = _contextService.GetShipment(shipmentId);

        if (shipment != null && !string.IsNullOrEmpty(shipment.Message))
        {
            _messageDistributor.SendShipmentUpdate(shipment);
        }
    }

    public override void UnsubscribedPacket(object? sender, UnsubscribedPacketEventArgs unsubscribedPacket)
    {
        if (TrackedPacketExists(unsubscribedPacket.PacketTracing))
        {
            var sealerRoute = _contextService.GetBoxSealerRoute(unsubscribedPacket.PacketTracing);

            if (!string.IsNullOrEmpty(sealerRoute))
            {
                var toSealerRoute = Diverters
                    .SelectMany(_ => _.Towards)
                    .First(_ => _.RoutePosition.Id == sealerRoute).RoutePosition.Name == DefaultRoute.BoxSealer.ToString();

                if (toSealerRoute)
                {
                    BoxLeftTheSealer(unsubscribedPacket);
                }
                else
                {
                    BoxIsRedirected(unsubscribedPacket);
                }
            }
        }
        else
            _logger.LogWarning($"The packet tracing ID {unsubscribedPacket.PacketTracing} could not be found in sector {this}");
    }

    private void BoxLeftTheSealer(UnsubscribedPacketEventArgs unsubscribedPacket)
    {
        try
        {
            _contextService.BoxLeftTheSealer(unsubscribedPacket.AtTime, unsubscribedPacket.PacketTracing);
            var shipment = _contextService.GetShipmentByPacketTracing(unsubscribedPacket.PacketTracing);
            _contextService.RemovePacketTracing(unsubscribedPacket.PacketTracing);

            _messageDistributor.SendShipmentUpdate(shipment);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.ToString());
        }

        RemoveTrackedPacket(unsubscribedPacket.PacketTracing);
    }

    private void BoxIsRedirected(UnsubscribedPacketEventArgs unsubscribedPacket)
    {
        //TODO: Noch offen..
        _contextService.RemovePacketTracing(unsubscribedPacket.PacketTracing);
        RemoveTrackedPacket(unsubscribedPacket.PacketTracing);
    }

    protected override void ErrorHandling(short errorCode)
    {
        var errorMessage = string.Empty;
        var faultIslandDestination = false;

        switch (errorCode)
        {
            case (short)Errorcode.EmergencyHold_Boxsealer:

                errorMessage = "";
                faultIslandDestination = false; //not required

                break;
        }

        UpdateShipmentsAfterError(errorMessage, faultIslandDestination);
    }

    private void UpdateShipmentsAfterError(string errorMsg, bool faultIslandDestination)
    {
        if (TrackedPackets.Any())
        {
            //TODO: Auch wenn sich die Packstücke zwischen Boxsealer und Flowsort befinden,
            //erhalten diese eine Faultdestination. Nachdem diese wieder rausgefahren sind,
            //werden die ursprunglichen Ziele vergeben: Funktion???

            var shipments = _contextService
                .GetShipmentsByPacketTracing(TrackedPackets.Select(_ => _.TracedPacketId).ToList());

            if (shipments.Any())
            {
                foreach (var shipment in shipments)
                {
                    if (faultIslandDestination)
                    {
                        _contextService.SetTarget(shipment.Id, CommonData.FaultIsland);
                    }

                    _contextService.SetMessage(errorMsg, shipment.Id);
                    _messageDistributor.SendShipmentUpdate(shipment);
                }
            }
        }
    }
}