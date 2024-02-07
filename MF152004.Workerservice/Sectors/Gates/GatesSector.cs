using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Models.Values.Types;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Services;

namespace MF152004.Workerservice.Sectors.Gates;

public abstract class GatesSector : Sector
{
    protected readonly ContextService _contextService;
    protected readonly MessageDistributor _messageDistributor;
    protected readonly PLC152004_PacketHelper _packetHelper = new();

    protected GatesSector(MqttClient client, ILogger<Sector> logger, string baseposition, string name, ContextService contextService,
        MessageDistributor messageDistributor) : base(client, logger, name, baseposition)
    {
        _contextService = contextService;
        _messageDistributor = messageDistributor;
        AddRelatedErrorCodes();
    }

    public override void AddRelatedErrorCodes()
    {
        var errors = new List<Errorcode>
        {
            Errorcode.EmergencyHold_TelescopicConveyor //TODO: Anpassen, ergänzen
        };

        RelatedErrorcodes.AddRange(errors.Cast<short>());
    }

    public override void Barcode_Scanned(object? sender, BarcodeScanEventArgs scan)
    {
        if (BarcodeScanner.BasePosition == scan.Position)
        {
            try
            {
                var shipment = _contextService.LogShipment(this, scan.Barcodes);

                if (shipment != null && scan.Barcodes?.Any(_ => _ == CommonData.NoRead) == true)
                {
                    _logger.LogInformation("{0}: NO_READ barcode detected: {1}", this, shipment);
                    shipment = null;
                }

                var shipmentId = shipment?.Id ?? -1;

                //TODO: Andere Tracings über shipmentID prüfen und wenn vorhanden löschen
                SetDiverterDirection(scan, shipmentId); //also the packet will be traced

                var diverter = Diverters
                    .FirstOrDefault(div => div.DriveDirection != div.Towards.First(t => t.FaultDirection).DriveDirection);

                if (diverter is null)
                {
                    _packetHelper.Create_NoExitFlowSortPosition(Diverters.First(), scan.PacketTracing); //go ahead
                    _logger.LogInformation($"{this}: The package {shipment}) will drive on");
                }
                else
                {
                    _packetHelper.Create_FlowSortPosition(diverter, scan.PacketTracing); //ausschleusen
                    _logger.LogInformation($"{this}: The package {shipment}) will drive out ({diverter.BasePosition}-{diverter.DriveDirection}))");
                }

                _client.SendData(_packetHelper.GetPacketData());

                ShipmentErrorHandling(shipmentId);
            }
            catch (Exception exception)
            {
                _logger.LogError($"({this}) - {exception}");
                return;
            }

            OnFaultyBarcodes(scan);
        }
    }

    private void SetDiverterDirection(BarcodeScanEventArgs scan, int shipmentId)
    {
        if (Diverters is null)
            throw new ArgumentNullException(nameof(Diverters));

        if (ValidShipmentId(shipmentId, scan.Barcodes?.ToArray()))
        {
            string[] destinationReferences = _contextService.GetDestinations(shipmentId);

            foreach (var diverter in Diverters)
            {
                var toward = diverter.Towards.FirstOrDefault(t => destinationReferences.Any(d => d == t.RoutePosition.Name));

                if (toward != null)
                {
                    if (ValidShippedStatus(shipmentId, toward) && LabelIsPrinted(shipmentId, toward) && !CapacityReached(toward) && TowardIsActive(toward))
                    {
                        AddTrackedPacket(scan.PacketTracing, shipmentId, toward.RoutePosition.Name);
                        _contextService.SetPacketTracing(scan.PacketTracing, scan.Barcodes?.ToArray() ?? new[] {""});
                        diverter.SetDirection(toward.DriveDirection);
                        return;
                    }
                }
            }
        }

        Diverters.ToList().ForEach(_ => _.SetFaultDirection());
    }

    private bool ValidShipmentId(int shipmentId, string[]? barcodes)
    {
        if (shipmentId < 1)
        {
            _logger.LogWarning($"The shipment ID is not valid in sector {this}. " +
                               $"Received Barcodes: {(barcodes != null ? string.Join(", ", barcodes) : "null")}");
            return false;
        }

        return true;
    }

    private bool ValidShippedStatus(int shipmentId, Toward toward)
    {
        if (!_contextService.IsShipped(shipmentId))
        {
            var msg = $"The package with ID {shipmentId} has the wrong status";
            var errorcode = "1007";

            _logger.LogWarning(errorcode + msg + $" in sector {this} ({toward.RoutePosition.Name})");
            _contextService.SetMessage(msg, shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);
            return false;
        }

        return true;
    }

    private bool LabelIsPrinted(int shipmentId, Toward toward)
    {
        if (!_contextService.LabelIsPrinted(shipmentId))
        {
            _logger.LogWarning($"The label has not printed. The package ID {shipmentId} " +
                               $"could not be ejected to the provided gate {toward.RoutePosition.Name}");
            return false;
        }

        return true;
    }

    private bool CapacityReached(Toward toward)
    {
        if (toward.RoutePosition.Destination.LoadFactor == 100)
        {
            _logger.LogWarning($"The load capacity of the gate " +
                               $"{toward.RoutePosition.Name} has been reached");
            return true;
        }

        return false;
    }
            

    private bool TowardIsActive(Toward toward)
    {
        if (!toward.RoutePosition.Destination.Active)
        {
            _logger.LogWarning($"The gate {toward.RoutePosition.Name} is not active");
            return false;
        }

        return true;
    }


    private void ShipmentErrorHandling(int shipmentId)
    {
        var shipment = _contextService.GetShipment(shipmentId);

        if (shipment != null && !string.IsNullOrEmpty(shipment.Message))
        {
            _messageDistributor.SendShipmentUpdate(shipment);
        }
    }

    private void OnFaultyBarcodes(BarcodeScanEventArgs scan)
    {
        if (scan.Barcodes is null)
        {
            _logger.LogWarning($"Barcodes is null on {this}");
        }
        else if (scan.Barcodes.Contains(CommonData.NoRead))
        {
            NoRead noRead = new()
            {
                AtTime = scan.AtTime,
                Position = scan.Position ?? string.Empty,
            };

            _messageDistributor.SendNoRead(noRead);

            _logger.LogWarning($"NOREAD was detected in barcodes (BCs: {string.Join(";", scan.Barcodes)})");
        }
    }

    public override void UnsubscribedPacket(object? sender, UnsubscribedPacketEventArgs unsubscribedPacket)
    {
        if (TrackedPacketExists(unsubscribedPacket.PacketTracing))
        {
            var shipmentId = _contextService.GetShipmentByPacketTracing(unsubscribedPacket.PacketTracing)?.Id;

            if (shipmentId != null)
            {
                var target = GetDestinationOfTrackedPacket(unsubscribedPacket.PacketTracing);

                if (target != null)
                {
                    _contextService.DestinationReached((int)shipmentId);
                    _contextService.SetTarget((int)shipmentId, target);
                    _contextService.RemovePacketTracing(unsubscribedPacket.PacketTracing);
                    RemoveTrackedPacket(unsubscribedPacket.PacketTracing);
                    _messageDistributor.SendShipmentUpdate(_contextService.GetShipment((int)shipmentId));
                }
            }
        }
        else
            _logger.LogWarning($"The packet tracing ID {unsubscribedPacket.PacketTracing} could not be found in sector {this}");
    }

    protected override void ErrorHandling(short errorCode)
    {

    }
}