using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Services;

namespace MF152004.Workerservice.Sectors.Gates;

public abstract class GatesSector : Sector
{
    protected readonly ContextService _contextService;
    protected readonly MessageDistributor _messageDistributor;

    protected GatesSector(MqttClient client, ILogger<Sector> logger, string basePosition, string name, ContextService contextService,
        MessageDistributor messageDistributor) : base(client, logger, name, basePosition)
    {
        _contextService = contextService;
        _messageDistributor = messageDistributor;
    }

    public override void AddRelatedErrorCodes()
    {
    }

    public override async void Barcode_Scanned(object? sender, BarcodeScanEventArgs scan)
    {
        if (BarcodeScanner.BasePosition == scan.Position)
        {
            try
            {
                var shipment = _contextService.LogShipment(this, scan.Barcodes);

                if (shipment == null)
                    return;

                if (scan.Barcodes?.Any(s => s == CommonData.NoRead) == true)
                {
                    _logger.LogInformation("{0}: NO_READ barcode detected: {1}", this, shipment);

                    await _messageDistributor.SendNoRead(new()
                    {
                        AtTime = scan.AtTime,
                        Position = scan.Position ?? string.Empty
                    });

                    return;
                }

                var shipmentDestinations = shipment.DestinationRouteReference?.Split(';').ToHashSet() ?? new HashSet<string>();
                var sectorDestinations = Diverters.SelectMany(d => d.Towards).Where(t => !t.FaultDirection).Select(t => t.RoutePosition.Name).Distinct().ToHashSet();
                
                // Are there any gates where we could possibly route?
                if (!shipmentDestinations.Any(sectorDestinations.Contains)) return;

                if (!_contextService.IsShipped(shipment) && shipment.DestinationRouteReference != CommonData.FaultIsland)
                {
                    _logger.LogWarning("{0}: Wrong shipment status ({1}), routing to the FaultIsland", this, shipment.Status);
                    shipment.Message = "Wrong shipment status";
                    _contextService.SetTarget(shipment, CommonData.FaultIsland);
                    return;
                }

                if (shipment.LabelPrintedAt == null)
                {
                    _logger.LogWarning("{0}: Label was not printed", this);
                    return;
                }

                foreach (var diverter in Diverters) diverter.SetFaultDirection();

                foreach (var diverter in Diverters)
                {
                    var toward = diverter.Towards.FirstOrDefault(t => shipmentDestinations.Contains(t.RoutePosition.Name));
                    
                    // There is no gate whee we could route
                    if (toward == null) continue;

                    if (!toward.RoutePosition.Destination.Active)
                        _logger.LogWarning("{0}: {1} - The gate is inactive", this, toward.RoutePosition.Name);

                    else if (toward.RoutePosition.Destination.LoadFactor >= 100 ) 
                        _logger.LogWarning("{0}: {1} - Maximum load capacity reached", this, toward.RoutePosition.Name);
                    
                    else
                    {
                        AddTrackedPacket(scan.PacketTracing, shipment.Id, toward.RoutePosition.Name);
                        shipment.PacketTracing = scan.PacketTracing;
                        diverter.SetDirection(toward.DriveDirection);
                        var packet = new PLC152004_PacketHelper();
                        packet.Create_FlowSortPosition(diverter, scan.PacketTracing);
                        _logger.LogInformation($"{this}: The package {shipment} will drive out to {toward.RoutePosition.Name} ({diverter.BasePosition}-{diverter.DriveDirection}))");
                        await _client.SendData(packet.GetPacketData());

                        break;
                    }
                }

                if (!string.IsNullOrEmpty(shipment.Message)) 
                    await _messageDistributor.SendShipmentUpdate(shipment);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "{0}: routing failed", this);
            }
        }
    }

    public override void UnsubscribedPacket(object? sender, UnsubscribedPacketEventArgs unsubscribedPacket)
    {
        if (TrackedPacketExists(unsubscribedPacket.PacketTracing))
        {
            var shipment = _contextService.GetShipmentByPacketTracing(unsubscribedPacket.PacketTracing);

            if (shipment != null)
            {
                var target = GetDestinationOfTrackedPacket(unsubscribedPacket.PacketTracing);

                if (target != null)
                {
                    _contextService.DestinationReached(shipment.Id);

                    // VK: Only setting final target if it was present in the original destination list
                    if (shipment.DestinationRouteReference?.Split(';').Any(s => s.Trim() == target) == true)
                        _contextService.SetTarget(shipment.Id, target);
                    
                    _contextService.RemovePacketTracing(unsubscribedPacket.PacketTracing);
                    RemoveTrackedPacket(unsubscribedPacket.PacketTracing);
                    _messageDistributor.SendShipmentUpdate(shipment);
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