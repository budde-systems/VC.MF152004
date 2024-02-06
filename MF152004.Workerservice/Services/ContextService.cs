using System.Diagnostics;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Models.EventArgs;
using MF152004.Models.Main;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Data;

namespace MF152004.Workerservice.Services;

public class ContextService //TODO: IShipment vervollständigen und diese Klassen hier in Models oder Commmon aufnehmen. Service soll mindestens IShipment-Daten zurückgeben.
{
    public ConfigurationService ConfigService { get; set; }

    private readonly ILogger<ContextService> _logger;

    private Context _context = new();
    private static object _contextLock = new();

    public ContextService(ILogger<ContextService> logger)
    {
        _logger = logger;
        ConfigService = new(_context);
    }

    public Context Context
    {
        get
        {
            lock (_contextLock )
            {
                return _context;
            }
        }

        set
        {
            lock (_contextLock)
            {
                _context = value;
            }
        }
    }

    public bool ContextHasRequiredEntities() => ConfigService.ConfigHasEntities() && ShipmentHasEntities();

    public bool ShipmentHasEntities() => Context.Shipments != null && Context.Shipments.Any();

    public void AddShipment(Shipment shipment)
    {
        lock (_contextLock)
        {
            _context.Shipments.Add(shipment);
            _logger.LogInformation("Shipment added: {0}", shipment);
        }
    }

    public void AddShipments(IList<Shipment>? shipments)
    {
        if (shipments == null || !shipments.Any()) return;

        if (shipments.Count == 1)
        {
            AddShipment(shipments[0]);
        }
        else
        {
            lock (_contextLock)
            {
                var sw = Stopwatch.StartNew();
                var existingShipments = _context.Shipments.Select(_ => _.TransportationReference).ToHashSet();
                _context.Shipments.AddRange(shipments.Where(_ => !existingShipments.Contains(_.TransportationReference)).ToList());
                _logger.LogInformation("{0} shipments added in {1} ms", shipments.Count, sw.ElapsedMilliseconds);
            }
        }
    }

    public void UpdateShipments(params Shipment[]? shipments)
    {
        if (shipments is null || !shipments.Any()) return;

        if (!Context.Shipments.Any())
        {
            AddShipments(shipments);
        }
        else
        {
            foreach (var shipment in shipments)
            {
                var index = Context.Shipments.FindIndex(s => s.Id == shipment.Id);
                    
                if (index == -1)
                {
                    AddShipment(shipment);
                }
                else
                {
                    if (Context.Shipments[index].DestinationRouteReference != shipment.DestinationRouteReference)
                        shipment.DestinationRouteReferenceUpdatedAt = DateTime.Now;

                    if (Context.Shipments[index].PacketTracing > 0)
                        shipment.PacketTracing = Context.Shipments[index].PacketTracing;

                    lock (_contextLock)
                    {
                        Context.Shipments[index] = shipment;
                    }
                }
            }
        }
    }

    /// <summary>
    /// If the packet tracing does not lead to a result, this function is not executed.
    /// </summary>
    /// <param name="atTime"></param>
    /// <param name="packetTracing"></param>
    public void BoxLeftTheSealer(DateTime atTime, int packetTracing)
    {
        var shipment = GetShipmentByPacketTracing(packetTracing);

        if (shipment != null)
            shipment.LeftSealerAt = atTime;
    }

    public void BoxHasBeenBranded_1(string transportReference) => BoxHasBeenBranded(transportReference, 1);
    public void BoxHasBeenBranded_1(int shipmentId) => BoxHasBeenBranded(shipmentId, 1);

    public void BoxHasBeenBranded_2(string transportReference) => BoxHasBeenBranded(transportReference, 2);
    public void BoxHasBeenBranded_2(int shipmentId) => BoxHasBeenBranded(shipmentId, 2);

    private void BoxHasBeenBranded(string transportReference, int machine)
    {
        var shipment = GetShipment(transportReference);

        if (shipment is null)
            return;

        lock (_contextLock)
        {
            if (machine == 1)
                shipment.BoxBrandedAt_1 = DateTime.Now;
            else
                shipment.BoxBrandedAt_2 = DateTime.Now;
        }
    }

    private void BoxHasBeenBranded(int shipmentId, int machine)
    {
        var shipment = GetShipment(shipmentId);

        if (shipment is null)
            return;

        if (machine == 1)
            shipment.BoxBrandedAt_1 = DateTime.Now;
        else
            shipment.BoxBrandedAt_2 = DateTime.Now;
    }

    /// <summary>
    /// The destination route and this date of the shipment will be updated if shipment exists.
    /// </summary>
    /// <param name="shipmentId"></param>
    /// <param name="target"></param>
    public void SetTarget(int shipmentId, string target)
    {
        var shipment = GetShipment(shipmentId);

        if (shipment is null)
        {
            //Log
        }
        else
        {
            shipment.DestinationRouteReference = target;
            _logger.LogInformation("Destination updated in {0}: {1} (4)", shipment, shipment.DestinationRouteReference);

            shipment.DestinationRouteReferenceUpdatedAt = DateTime.Now;
        }
    }

    public Shipment? GetShipment(int id) => Context.Shipments.SingleOrDefault(x => x.Id == id);

    public int GetShipmentId(params string[] barcodes) => GetShipmentByTransportationReference(barcodes)?.Id ?? 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transportReference"></param>
    /// <param name="trackingCode"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Shipment? GetShipment(string transportReference = "", string trackingCode = "")
    {
        if (!string.IsNullOrEmpty(transportReference))
            return Context.Shipments.SingleOrDefault(x => x.TransportationReference == transportReference);

        if (!string.IsNullOrEmpty(trackingCode))
            return Context.Shipments.SingleOrDefault(x => x.TrackingCode == trackingCode);

        throw new ArgumentException("At least one parameter must have a value");
    }

    public Shipment? GetShipmentByTransportationReference(IList<string>? barcodes)
    {
        return barcodes == null ? null : Context.Shipments.SingleOrDefault(x => barcodes.Any(b => b == x.TransportationReference));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="barcodes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool BoxHasNotChanged(params string[] barcodes)
    {
        if (barcodes is null)
            throw new ArgumentNullException();

        var shipment = GetShipmentByTransportationReference(barcodes);

        if (shipment == null)
            return false;

        return barcodes.Any(x => x == shipment.BoxBarcodeReference);
    }

    public bool IsShipped(params string[] barcodes) => GetShipmentByTransportationReference(barcodes)?.Status == "shipped"; //TODO: enum??

    public bool IsShipped(int shipmentId) => GetShipment(shipmentId)?.Status == "shipped";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="barcodes"></param>
    /// <returns>null or empty string if something went wrong</returns>
    public string? GetBoxSealerRoute(params string[]? barcodes) //TODO: logging
    {
        if (barcodes is null)
            return string.Empty;

        var sealerRoute = Context?.Config?.SealerRouteConfigs?
            .FirstOrDefault(x => barcodes.Any(bc => bc == x.BoxBarcodeReference))?.SealerRouteReference;

        if (string.IsNullOrEmpty(sealerRoute))
        {
            var shipment = GetShipmentByTransportationReference(barcodes);

            if (shipment == null)
                return string.Empty;
            
            sealerRoute = Context?.Config?.SealerRouteConfigs?
                .FirstOrDefault(x => x.BoxBarcodeReference == shipment.BoxBarcodeReference)?.SealerRouteReference;
        }

        return sealerRoute;
    }

    public string? GetBoxSealerRoute(int packetTracing) =>
        GetBoxSealerRoute(GetShipmentByPacketTracing(packetTracing)?.BoxBarcodeReference ?? "");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="barcodes"></param>
    /// <returns>null or an empty string if something went wrong</returns>
    public string? GetLabelPrinterReference(params string[]? barcodes)
    {
        if (barcodes is null || barcodes.Length == 0)
            return string.Empty;

        var printerReference = Context?.Config?.LablePrinterConfigs?
            .FirstOrDefault(x => barcodes.Any(bc => bc == x.BoxBarcodeReference))?.LabelPrinterReference;

        if (string.IsNullOrEmpty(printerReference))
        {
            var shipment = GetShipmentByTransportationReference(barcodes);

            if (shipment is null)
                return string.Empty;
            
            printerReference = Context?.Config?.LablePrinterConfigs?
                .FirstOrDefault(x => x.BoxBarcodeReference == shipment.BoxBarcodeReference)?.LabelPrinterReference;
        }

        return printerReference;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="barcodes"></param>
    /// <returns>An empty string if something went wrong, otherwise the destination route</returns>
    public string GetDestination(params string[]? barcodes)
    {
        if (barcodes is null)
            return string.Empty;

        var shipment = GetShipmentByTransportationReference(barcodes);

        if (shipment is null)
            return string.Empty;

        return shipment.DestinationRouteReference ?? string.Empty;
    }

    /// <summary>
    /// Many possible destination route references will be stored, splitted by ';'<br/>
    /// The webservice makes the decision about the routes
    /// </summary>
    /// <param name="shipmentId"></param>
    /// <returns>An empty array if something went wrong, otherwise the destination routes</returns>
    public string[] GetDestinations(int shipmentId) => GetShipment(shipmentId)?.DestinationRouteReference?.Split(';') ?? Array.Empty<string>();

    /// <summary>
    /// The realationship between trackingcode and transport code / reference
    /// </summary>
    /// <param name="shipmentId"></param>
    /// <param name="barcodes"></param>
    /// <returns></returns>
    public bool RelationShipIsValid(int shipmentId, params string[]? barcodes)
    {
        if (barcodes is null || barcodes.Length == 0)
            return false;

        var shipment = GetShipment(shipmentId);

        if (shipment is null)
            return false;

        return shipment.TrackingCode != null && barcodes.Any(bc => bc.ToLower().Contains(shipment.TrackingCode.ToLower()));
    }

    public void LabelPrintedAt(int shipmentId, bool fail)
    {
        var shipment = GetShipment(shipmentId);

        if (shipment != null)
        {
            if (fail)
                shipment.LabelPrintingFailedAt = DateTime.Now;
            else
                shipment.LabelPrintedAt = DateTime.Now;
        }
    }

    public bool LabelIsPrinted(int shipmentId)
    {
        var shipment = GetShipment(shipmentId);
        return shipment is { LabelPrintedAt: not null };
    }

    public void SetMessage(string message, params string[] barcodes)
    {
        var shipment = GetShipmentByTransportationReference(barcodes);

        if (shipment is null)
            return;

        shipment.Message = message;
    }

    public void SetMessage(string message, int shipmentId)
    {
        var shipment = GetShipment(shipmentId);

        if (shipment != null)
            shipment.Message = message;
    }

    public bool WeightIsValid(double isWeight, int shipmentId)
    {
        var shipment = GetShipment(shipmentId);

        if (shipment is null) //TODO: Log
            return false;

        var plusTolerance = (shipment.Weight * 1000) + CommonData.WeightTolerance;
        var minusTolerance = (shipment.Weight * 1000) - CommonData.WeightTolerance;

        if (plusTolerance < 0 || minusTolerance < 0) //The less than zero check was removed at the request of the customer
        {
            _logger.LogWarning($"The tolerance is lesser than 0. Values: " +
                               $"positive tolerance + IS-weight = {plusTolerance}; " +
                               $"negative tolerance + IS-weight = {minusTolerance}");
        }

        return isWeight <= plusTolerance && isWeight >= minusTolerance;
    }

    public void AddWeightScan(Scan scan)
    {
        lock (_contextLock)
        {
            _context.WeightScans.Add(scan);
        }
    }

    public Scan? GetWeightScan(int shipmentId) => Context.WeightScans.SingleOrDefault(x => x.ShipmentId == shipmentId);

    public double GetIsWeight(int shipmentId) => GetWeightScan(shipmentId)?.Weight ?? 0;

    public string GetBrandingReferenceId(int? shipmentId)
    {
        if (shipmentId == null)
            return string.Empty;

        var shipment = GetShipment((int)shipmentId);

        return shipment == null ? string.Empty : BrandingPdfReferenceId(shipment);
    }

    public string GetBrandingReferenceIdByPacketTracing(int? packetTracing)
    {
        if (packetTracing == null)
            return string.Empty;

        var shipment = GetShipmentByPacketTracing((int)packetTracing);

        if (shipment == null)
            return string.Empty;

        return BrandingPdfReferenceId(shipment);
    }

    /// <summary>
    /// First: matching by barcodes. On fail: matching by shipment 
    /// </summary>
    /// <param name="barcodes"></param>
    /// <returns>An empty string if something went wrong</returns>
    public string GetBrandingReferenceId(params string[]? barcodes) //prio 1
    {
        if (barcodes == null)
            return string.Empty;

        var shipment = GetShipmentByTransportationReference(barcodes);

        if (shipment == null)
            return string.Empty;

        var refId = Context.Config.BrandingPdfConfigs
            .FirstOrDefault(config => barcodes.Any(bc => bc == config.BoxBarcodeReference) &&
                                      config.ClientReference == shipment.ClientReference)?.BrandingPdfReference;

        if (refId is null)
            _logger.LogInformation($"The branding reference ID couldn't be found with received barcodes. " +
                                   $"Received BCs: {string.Join(", ", barcodes)}");

        return refId ?? BrandingPdfReferenceId(shipment);
    }

    private string BrandingPdfReferenceId(Shipment shipment)
    {
        var id = Context.Config.BrandingPdfConfigs
            .FirstOrDefault(_ => _.ClientReference == shipment.ClientReference && 
                                 _.BoxBarcodeReference == shipment.BoxBarcodeReference)?.BrandingPdfReference ?? string.Empty;

        return id;
    }

    public void SetPacketTracing(int packetTracing, params string[] barcodes)
    {
        var shipment = GetShipmentByTransportationReference(barcodes);

        if (shipment is null) //TODO: Log
            return;

        shipment.PacketTracing = packetTracing;
    }

    public void RemovePacketTracing(params string[] barcodes)
    {
        var shipment = GetShipmentByTransportationReference(barcodes);

        if (shipment is null) //TODO: Log
            return;

        shipment.PacketTracing = 0;
    }

    public void RemovePacketTracing(int packetTracing)
    {
        var shipment = GetShipmentByPacketTracing(packetTracing);

        if (shipment is null) //TODO: Log
            return;

        shipment.PacketTracing = 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="packetTracing"></param>
    /// <returns></returns>
    public Shipment? GetShipmentByPacketTracing(int packetTracing) =>
        Context.Shipments?.FirstOrDefault(_ => _.PacketTracing == packetTracing); //TODO: Single hat Fehler ergaben wegen doppelten Eintrag??

    public ICollection<Shipment> GetShipmentsByPacketTracing(List<int> tracedPackets) =>
        Context.Shipments?.Where(_ => tracedPackets.Contains(_.PacketTracing)).ToList() ?? new List<Shipment>();

    /// <summary>
    /// Get all shipments which have not yet reached their destination
    /// </summary>
    /// <returns></returns>
    public ICollection<Shipment> GetRunningShipments() =>
        Context.Shipments?.Where(_ => _.DestinationReachedAt is null).ToList() ?? new List<Shipment>();

    public void DestinationReached(int shipmentId)
    {
        var shipment = GetShipment(shipmentId);

        if (shipment != null)
            shipment.DestinationReachedAt = DateTime.Now;
    }

    #region Event-subscribers

    public void UpdateShipments(object? sender, UpdateShipmentEventArgs shipments) =>
        UpdateShipments(shipments.UpdatedShipments?.ToArray());

    public void NewShipments(object? sender, NewShipmentEventArgs shipments) =>
        AddShipments(shipments.NewShipments);

    public void UpdateConfiguration(object? sender, UpdateConfigurationEventArgs configs) =>
        ConfigService.UpdateConfigs(configs.ServiceConfiguration);

    #endregion

    public Shipment? LogShipment(Sector sector, IList<string>? barcodes)
    {
        if (barcodes == null || !barcodes.Any())
        {
            _logger.LogWarning("{0}: Empty Barcodes", sector);
            return null;
        }

        var shipment = GetShipmentByTransportationReference(barcodes?.ToArray());
        
        if (shipment == null)
            _logger.LogWarning("{0}: Unknown shipment detected. Barcodes: {1}", sector, string.Join(", ", barcodes!));
        else
            _logger.LogInformation("{0}: Shipment: {1}", sector, shipment);

        return shipment;
    }
}