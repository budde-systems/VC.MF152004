using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Common.Connection.Clients;
using MF152004.Models.EventArgs;
using MF152004.Models.Values.Types;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Services;

namespace MF152004.Workerservice.Sectors;

//TODO: Die Möglichkeit den Sektor zu aktivieren und deaktivieren
public class BrandPrinterSector : Sector //TODO: Sector als Base verwenden, dann Art der Maschine mit default methods und dann richtiges Objekt
{
    private const string NAME = "Brand printers";

    private readonly ContextService _contextService;
    private readonly MessageDistributor _messageDistributor;

    private BrandingPrinterClient _brandingPrinterClient;

    public BrandPrinterSector(IClient client, string basePosition, ContextService contextService,
        MessageDistributor messageDistributor) : base(client, NAME, basePosition)
    {
        _contextService = contextService;
        _messageDistributor = messageDistributor;
        AddRelatedErrorCodes();
        IsActive = true;
    }

    public override void AddRelatedErrorCodes()
    {
        var errors = new List<ErrorCode>
        {
            ErrorCode.EmergencyHold_BrandPrinter1,
            ErrorCode.EmergencyHold_Brandprinter2,
            ErrorCode.LowPrinterToner1,
            ErrorCode.LowPrinterToner2, //TODO: weitere ergänzen
        };

        RelatedErrorCodes.AddRange(errors.Cast<short>());
    }

    public void AddBrandingPrinterClient(BrandingPrinterClient brandPrinterClient)
    {
        if (brandPrinterClient != null)
        {
            if (brandPrinterClient.BrandPrinters.Any())
            {
                BarcodeScanners ??= new();
                BarcodeScanners.AddRange(brandPrinterClient.BrandPrinters.Select(x => x.RelatedScanner));
                _brandingPrinterClient = brandPrinterClient;
                _brandingPrinterClient.EndOfPrint += UpdateShipment;
            }
        }
    }

    public override List<IDiverter> CreateDiverters() => new(); //no diverters required in this sector

    public override Scanner CreateScanner() => new("", ""); //not required because of many scanners

    public override void Barcode_Scanned(object? sender, BarcodeScanEventArgs scan)
    {
        if (BarcodeScanners != null && BarcodeScanners.Any(_ => _.BasePosition == scan.Position))
        {
            if (!IsActive)
            {
                _logger.LogInformation("BrandPrinters are deactivated");
                return;
            }

            if (_brandingPrinterClient is null)
            {
                _logger.LogWarning("The brandingprinter client is null");
                return;
            }

            var shipmentId = ValidateBarcodesAndGetShipmentId(scan.Barcodes?.ToArray());

            if (shipmentId > 0)
            {
                var referenceId = _contextService.GetBrandingReferenceId(scan.Barcodes?.ToArray());

                if (!string.IsNullOrEmpty(referenceId))
                {
                    _brandingPrinterClient.Print(scan.Position, referenceId, shipmentId);
                    return;
                }
                else
                {
                    _logger.LogWarning($"The reference ID is empty. Print can't be executed on position {scan.Position}");
                }
            }

            _brandingPrinterClient.TransparentPrint(scan.Position, shipmentId);
            OnFaultyBarcodes(scan);
        }
    }

    private void UpdateShipment(object? sender, FinishedPrintJobEventArgs finishedJob)
    {
        if (finishedJob != null)
        {
            if (finishedJob.BasePositionBrandPrinter == BrandPrinterPosition.BP1.ToString())
                _contextService.BoxHasBeenBranded_1(finishedJob.Job.ShipmentId);
            
            else if (finishedJob.BasePositionBrandPrinter == BrandPrinterPosition.BP2.ToString())
                _contextService.BoxHasBeenBranded_1(finishedJob.Job.ShipmentId);

            else
                return;

            _messageDistributor.SendShipmentUpdate(_contextService.GetShipment(finishedJob.Job.ShipmentId));
        }
    }

    private int ValidateBarcodesAndGetShipmentId(params string[]? barcodes)
    {
        if (barcodes is null || barcodes.Any(_ => _ == CommonData.NoRead))
        {
            _logger.LogWarning($"Shipment can't be validate in: {this} " +
                               $"Received barcodes: {(barcodes != null ? string.Join(", ", barcodes) : "null")}");
            return -1;
        }

        return _contextService.GetShipmentId(barcodes);
    }

    public override void UnsubscribedPacket(object? sender, UnsubscribedPacketEventArgs unsubscribedPacket)
    {
        //NOT REQUIRED

        //var brandPrinter = _brandPrinters.SingleOrDefault(_ => _.TracedPacketExists(unsubscribedPacket.PacketTracing));

        //if (brandPrinter != null)
        //{
        //    if (brandPrinter.BasePosition == "BP1")
        //        _contextService.BoxHasBeenBranded_1(unsubscribedPacket.PacketTracing);
        //    else
        //        _contextService.BoxHasBeenBranded_2(unsubscribedPacket.PacketTracing);

        //    brandPrinter.RemoveTracedPacket(unsubscribedPacket.PacketTracing);
                
        //    var shipment = _contextService.GetShipment(unsubscribedPacket.PacketTracing);
        //    _messageDistributor.SendShipmentUpdate(shipment);
        //}
        //else
        //{
        //    _logger.LogWarning("No packet is traced by ID " + unsubscribedPacket.PacketTracing);
        //}            
    }

    private void ErrorHandling(object? sender, BrandPrinterErrorEventArgs error)
    {
        //NOT REQUIRED

        //var brandPrinter = _brandPrinters.SingleOrDefault(_ => _.Name == error.BrandprinterName);

        //if (brandPrinter != null)
        //{
        //    UpdateShipmentsOnError(brandPrinter.TracedPackets, $"Fehler beim Logodrucker {brandPrinter.Name}");
        //}
    }

    private void UpdateShipmentsOnError(List<int> tracedPackets, string errorMsg)
    {
        foreach (var shipmentId in tracedPackets)
        {
            if (shipmentId > 0)
            {
                _contextService.SetMessage(errorMsg, shipmentId);
                _messageDistributor.SendShipmentUpdate(_contextService.GetShipment(shipmentId));
            }
        }
    }

    protected override void ErrorHandling(short errorCode)
    {
        //TODO: Offen
    }

    private void OnFaultyBarcodes(BarcodeScanEventArgs scan)
    {
        if (scan.Barcodes is null)
        {
            _logger.LogError("The barcodes of scan is null");
        }
        else if (scan.Barcodes.Contains(CommonData.NoRead))
        {
            NoRead noRead = new()
            {
                AtTime = scan.AtTime,
                Position = scan.Position ?? "unknown"
            };

            _messageDistributor.SendNoRead(noRead);
        }
    }
}