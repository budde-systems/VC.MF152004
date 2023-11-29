using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Models.Types;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Common.Data;
using MF152004.Models.EventArgs;
using MF152004.Models.Main;
using MF152004.Models.Values.Types;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Services;

namespace MF152004.Workerservice.Sectors;

public class ScaleSector : Sector
{
    private const string NAME = "ScaleSection";

    private readonly ContextService _contextService;
    private readonly PLC152004_PacketHelper _packetHelper = new();
    private readonly MessageDistributor _messageDistributor;

    public ScaleSector(IClient client, string basePosition, ContextService contextService,
        MessageDistributor messageDistributor) : base(client, NAME, basePosition)
    {
        _contextService = contextService;
        _messageDistributor = messageDistributor;
        AddRelatedErrorCodes();
        Diverters = CreateDiverters();
    }

    public override void AddRelatedErrorCodes()
    {
        var errors = new List<ErrorCode>
        {
            ErrorCode.EmergencyHold_Scale //TODO: weitere ergänzen
        };

        RelatedErrorCodes.AddRange(errors.Cast<short>());
    }

    public override ICollection<IDiverter> CreateDiverters()
    {
        var flowSort = new FlowSort
        {
            Name = NAME,
            BasePosition = "3.2.80",
            SubPosition = "3.2.79"
        };

        RoutePosition routePosition = new();
        routePosition.SetRoutePosition(new Destination { Name = CommonData.FaultIsland });

        flowSort.CreateTowards(new[]
        {
            new Toward
            {
                DriveDirection = Direction.Left,
                FaultDirection = true,
                RoutePosition = routePosition
            },

            new Toward
            {
                DriveDirection = Direction.StraightAhead,
                RoutePosition = new RoutePosition
                {
                    Id = "1",
                    Name = DefaultRoute.ToGates.ToString()
                }
            }
        });

        List<IDiverter> diverters = new() { flowSort };

        return diverters;
    }

    public override Scanner CreateScanner() => new("M3.2.192", "S3.2.193");

    public override void Barcode_Scanned(object? sender, BarcodeScanEventArgs scan)
    {
        //not required
    }

    public override void Weight_Scanned(object? sender, WeightScanEventArgs scan)
    {
        if (scan is WeightScanEventArgs_152004 specialScan)
        {
            var diverter = Diverters.FirstOrDefault();

            try
            {
                var shipmentId = ValidateBarcodesAndGetShipmentId(specialScan.Barcodes?.ToArray());
                SetDiverterDirection(diverter, specialScan, shipmentId);

                _packetHelper.Create_FlowSortPosition(diverter, specialScan.PacketTracing);
                    
                _contextService.SetPacketTracing(specialScan.PacketTracing, specialScan.Barcodes?.ToArray() ?? new[] { "" });
                AddTrackedPacket(specialScan.PacketTracing, shipmentId);

                _client.SendData(_packetHelper.GetPacketData());

                ShipmentErrorHandling(shipmentId);
                WeightScanHandling(shipmentId, specialScan.Weight, specialScan.AtTime);

            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
            }

            OnFaultyBarcodes(specialScan); //handling of noreads
        }
    }

    private void OnFaultyBarcodes(WeightScanEventArgs_152004 specialScan)
    {
        if (specialScan.Barcodes is null)
        {
            _logger.LogWarning($"Barcodes is null in sector {this}");
        }
        else if (specialScan.Barcodes.Contains(CommonData.NoRead))
        {
            NoRead noRead = new()
            {
                AtTime = specialScan.AtTime,
                Position = specialScan.Position ?? "No Position",
            };

            _messageDistributor.SendNoRead(noRead);
        }
    }

    private void SetDiverterDirection(IDiverter? diverter, WeightScanEventArgs_152004 specialScan, int shipmentId)
    {
        if (diverter is null)
            throw new ArgumentNullException(nameof(diverter));

        if (InvalidShipmentId(shipmentId, specialScan.Barcodes?.ToArray()) || FaultDestination(shipmentId, diverter) || InvalidShippedStatus(shipmentId) || 
            InvalidWeight(shipmentId, specialScan.Weight) || InvalidHeight(shipmentId, specialScan.ValidHeight) || NoLabelPrint(shipmentId))
        {
            diverter.SetFaultDirection();
            return;
        }

        var toward = diverter.Towards
            .FirstOrDefault(_ => _.RoutePosition.Name == DefaultRoute.ToGates.ToString());

        if (toward is null) //TODO: Logging
        {
            diverter.SetFaultDirection();
            return;
        }

        diverter.SetDirection(toward.DriveDirection);
    }

    private int ValidateBarcodesAndGetShipmentId(string[]? barcodes)
    {
        if (barcodes is null || barcodes.Length == 0)
            return -1; //TODO: Logging
        
        return _contextService.GetShipmentId(barcodes);
    }

    private bool InvalidShipmentId(int shipmentId, string[]? barcodes)
    {
        if (shipmentId < 1)
        {
            _logger.LogWarning($"Invalid shipment ID in sector {this}. " +
                               $"Received barcodes: {(barcodes != null ? string.Join(", ", barcodes) : "null")}");
            return true;
        }

        return false;
    }

    private bool InvalidShippedStatus(int shipmentId)
    {
        if (!_contextService.IsShipped(shipmentId))
        {
            var msg = "The package has the wrong status";
            var errorcode = "1003";

            _contextService.SetMessage(errorcode + msg, shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);

            _logger.LogWarning(msg + $" ID: {shipmentId}");

            return true;
        }

        return false;
    }

    private bool InvalidWeight(int shipmentId, double isWeight)
    {
        if (!_contextService.WeightIsValid(isWeight, shipmentId))
        {
            _contextService.SetMessage($"1004The package differs from the TARGET weight. The actual weight is {isWeight}", shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);

            _logger.LogWarning($"Wrong weight for ID {shipmentId}");
            return true;
        }

        return false;
    }

    private bool InvalidHeight(int shipmentId, bool validHeight)
    {
        if (!validHeight)
        {
            var msg = "The package has not passed the height test";
            var errorCode = "1005";

            _contextService.SetMessage(errorCode + msg, shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);

            _logger.LogWarning($"{msg}. (ID: {shipmentId}, sector: {this})");
        }

        return !validHeight;
    }

    private bool NoLabelPrint(int shipmentId)
    {
        if (!FileManager.ZplExists(shipmentId))
        {
            var msg = "No zpl file has been found";
            var errorCode = "1006";

            _contextService.SetMessage(errorCode + msg, shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);

            _logger.LogWarning(msg + $" Shipment ID: {shipmentId}, sector: {this}");

            return true;
        }

        return false;
    }

    private bool FaultDestination(int shipmentId, IDiverter diverter)
    {
        var result = _contextService.GetDestinations(shipmentId).Contains(diverter.Towards.First(_ => _.FaultDirection).RoutePosition.Name);

        if (result)
            _logger.LogInformation($"Shipment {shipmentId} has a fault direction");

        return result;
    }

    private void ShipmentErrorHandling(int shipmentId)
    {
        var shipment = _contextService.GetShipment(shipmentId);

        if (shipment != null && !string.IsNullOrEmpty(shipment.Message))
        {
            _messageDistributor.SendShipmentUpdate(shipment);
        }
    }

    private void WeightScanHandling(int shipmentId, double scannedWeight, DateTime scanningTime)
    {
        if (shipmentId > 0)
        {
            var scan = new Scan
            {
                ScanTime = scanningTime,
                ShipmentId = shipmentId,
                Weight = scannedWeight,
                ScanType = _contextService.WeightIsValid(scannedWeight, shipmentId) ?
                    ScanType.successful_scan : ScanType.wrong_weight,
            };

            _messageDistributor.SendWeightScan(scan);
        }
    }

    public override void UnsubscribedPacket(object? sender, UnsubscribedPacketEventArgs unsubscribedPacket)
    {
        if (TrackedPacketExists(unsubscribedPacket.PacketTracing))
        {
            //TODO: Funktion offen
            RemoveTrackedPacket(unsubscribedPacket.PacketTracing);
            _contextService.RemovePacketTracing(unsubscribedPacket.PacketTracing);
        }
        else
            _logger.LogWarning($"The packet tracing ID {unsubscribedPacket.PacketTracing} could not be found in sector {this}");
    }

    protected override void ErrorHandling(short errorCode)
    {
        var errorMessage = string.Empty;
        var faultIslandDestination = false;

        switch (errorCode)
        {
            case (short)ErrorCode.EmergencyHold_Scale:

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
            //TODO: Offene Funktion
        }
    }
}