using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Common.Machines;
using MF152004.Models.Settings.BrandPrinter;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Services;

namespace MF152004.Workerservice.Sectors;

public class BrandPrinterSector : Sector
{
    private const string NAME = "Brand printers";

    private readonly ContextService _contextService;
    private readonly MessageDistributor _messageDistributor;

    private readonly Dictionary<string, BrandPrinter> _printers;

    private readonly BrandPrinter _printerFront;
    private readonly BrandPrinter _printerBack;

    public BrandPrinterSector(MqttClient client, ILogger<Sector> logger, string basePosition, ContextService contextService,
        MessageDistributor messageDistributor, IConfiguration configuration) : base(client, logger, NAME, basePosition)
    {
        _contextService = contextService;
        _messageDistributor = messageDistributor;

        _printers = new()
        {
            {
                "M3.1.189", 
                _printerFront = new BrandPrinter
                {
                    Name = "BrandPrinter Front",
                    Settings = configuration.GetSection("brand_printer_config_front").Get<BrandPrinterSettings>() ?? throw new Exception("'brand_printer_config_front' section was not found in configuration")
                }
            },
            {
                "M3.1.211",
                _printerBack = new BrandPrinter
                {
                    Name = "BrandPrinter Back",
                    Settings = configuration.GetSection("brand_printer_config_back").Get<BrandPrinterSettings>() ?? throw new Exception("'brand_printer_config_back' section was not found in configuration")
                }
            }
        };

        IsActive = true;
    }

    public override void AddRelatedErrorCodes()
    {
    }

    public override List<IDiverter> CreateDiverters() => new(); //no diverters required in this sector

    public override Scanner CreateScanner() => new("", ""); //not required because of many scanners

    public override async void Barcode_Scanned(object? sender, BarcodeScanEventArgs scan)
    {
        var printer = _printers.GetValueOrDefault(scan.Position ?? string.Empty);

        if (printer == null || scan.Barcodes == null || !scan.Barcodes.Any())
            return;

        if (!IsActive)
        {
            _logger.LogInformation("BrandPrinters are deactivated");
            return;
        }

        try
        {
            var shipment = _contextService.LogShipment(this, scan.Barcodes);

            if (shipment != null && scan.Barcodes?.Contains(CommonData.NoRead) == true)
            {
                _logger.LogInformation("{0}: NO_READ barcode detected: {1}", this, shipment);

                if (scan.Barcodes?.Contains(CommonData.NoRead) == true)
                {
                    await _messageDistributor.SendNoRead(new()
                    {
                        AtTime = scan.AtTime,
                        Position = scan.Position ?? "unknown"
                    });
                }

                shipment = null;
            }

            var referenceId = (shipment != null
                ? _contextService.GetBrandingReferenceId(scan.Barcodes!, shipment)
                : null) ?? printer.Settings.Configuration.NoPrintValue;

            if (shipment != null)
            {
                //if (printer == _printerFront && shipment.BoxBrandedAt_1.HasValue)
                //{
                //    _logger.LogInformation("{0}: Shipment {1} was already branded at {2}, skipping printing", printer, shipment, shipment.BoxBrandedAt_1.Value);
                //    return;
                //}

                //if (printer == _printerBack && shipment.BoxBrandedAt_2.HasValue)
                //{
                //    _logger.LogInformation("{0}: Shipment {1} was already branded at {2}, skipping printing", printer, shipment, shipment.BoxBrandedAt_2.Value);
                //    return;
                //}

                _logger.LogInformation("{0}: Printing ref {1} for shipment {2}", printer, referenceId, shipment);
            }
            else
                _logger.LogInformation("{0}: Printing ref {1} for barcodes {2}", printer, referenceId, string.Join(", ", scan.Barcodes!));

            await printer.Print(referenceId);
            _logger.LogInformation("{0}: Printing done", printer);

            if (shipment != null && printer == _printerFront)
            {
                shipment.BoxBrandedAt_1 = DateTime.Now;
                await _messageDistributor.SendShipmentUpdate(shipment);
            }
            else if (shipment != null && printer == _printerBack)
            {
                shipment.BoxBrandedAt_2 = DateTime.Now;
                await _messageDistributor.SendShipmentUpdate(shipment);
            }

            if (referenceId != printer.Settings.Configuration.NoPrintValue)
            {
                await Task.Delay(5000);
                _logger.LogInformation("{0}: Setting ref {1}", printer, printer.Settings.Configuration.NoPrintValue);
                await printer.Print(printer.Settings.Configuration.NoPrintValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{0} Printing failed for scan {1}", printer, string.Join(", ", scan.Barcodes!));
        }
    }

    public override void UnsubscribedPacket(object? sender, UnsubscribedPacketEventArgs unsubscribedPacket)
    {
    }

    protected override void ErrorHandling(short errorCode)
    {
    }
}