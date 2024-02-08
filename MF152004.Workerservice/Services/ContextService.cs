using System.Diagnostics;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Models.EventArgs;
using MF152004.Models.Main;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Data;

namespace MF152004.Workerservice.Services;

public class ContextService
{
    public ConfigurationService ConfigService { get; set; }

    private readonly ILogger<ContextService> _logger;

    private static object _contextLock = new();

    public ContextService(ILogger<ContextService> logger)
    {
        _logger = logger;
        ConfigService = new(Context);
    }

    public Context Context { get; } = new();

    public bool ContextHasRequiredEntities() => ConfigService.ConfigHasEntities() && ShipmentHasEntities();

    public bool ShipmentHasEntities() => Context.Shipments.Any();

    public void AddShipment(Shipment shipment)
    {
        lock (_contextLock)
        {
            Context.Shipments.Add(shipment);
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
                var existingShipments = Context.Shipments.Select(s => s.TransportationReference).ToHashSet();
                Context.Shipments.AddRange(shipments.Where(s => !existingShipments.Contains(s.TransportationReference)).ToList());
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
            shipment.DestinationRouteReferenceUpdatedAt = DateTime.Now;

            _logger.LogInformation("Destination updated in {0}: {1} (4)", shipment, shipment.DestinationRouteReference);
        }
    }

    public Shipment? GetShipment(int id) => Context.Shipments.FirstOrDefault(x => x.Id == id);

    public int GetShipmentId(params string[] barcodes) => GetShipmentByTransportationReference(barcodes)?.Id ?? 0;

    public Shipment? GetShipmentByTransportationReference(IList<string>? barcodes)
    {
        return barcodes == null ? null : Context.Shipments.FirstOrDefault(x => barcodes.Any(b => b == x.TransportationReference));
    }

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

        var sealerRoute = Context.Config.SealerRouteConfigs
            .FirstOrDefault(x => barcodes.Any(bc => bc == x.BoxBarcodeReference))?.SealerRouteReference;

        if (string.IsNullOrEmpty(sealerRoute))
        {
            var shipment = GetShipmentByTransportationReference(barcodes);

            if (shipment == null)
                return string.Empty;
            
            sealerRoute = Context.Config.SealerRouteConfigs
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

        var printerReference = Context.Config.LabelPrinterConfigs
            .FirstOrDefault(x => barcodes.Any(bc => bc == x.BoxBarcodeReference))?.LabelPrinterReference;

        if (string.IsNullOrEmpty(printerReference))
        {
            var shipment = GetShipmentByTransportationReference(barcodes);

            if (shipment is null)
                return string.Empty;
            
            printerReference = Context.Config.LabelPrinterConfigs
                .FirstOrDefault(x => x.BoxBarcodeReference == shipment.BoxBarcodeReference)?.LabelPrinterReference;
        }

        return printerReference;
    }
    
    /// <summary>
    /// Many possible destination route references will be stored, split by ';'<br/>
    /// The webservice makes the decision about the routes
    /// </summary>
    /// <param name="shipmentId"></param>
    /// <returns>An empty array if something went wrong, otherwise the destination routes</returns>
    public string[] GetDestinations(int shipmentId) => GetShipment(shipmentId)?.DestinationRouteReference?.Split(';') ?? Array.Empty<string>();

    /// <summary>
    /// The relationship between tracking code and transport code / reference
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

    /// <summary>
    /// First: matching by barcodes. On fail: matching by shipment 
    /// </summary>
    /// <param name="barcodes"></param>
    /// <param name="shipment"></param>
    /// <returns>An empty string if something went wrong</returns>
    public string? GetBrandingReferenceId(IList<string> barcodes, Shipment shipment)
    {
        var clientRefs = Context.Config.BrandingPdfConfigs.Where(config => config.ClientReference == shipment.ClientReference).ToArray();

        return (clientRefs.FirstOrDefault(c => barcodes.Any(bc=>bc == c.BoxBarcodeReference))
            ?? clientRefs.FirstOrDefault(c=>c.BoxBarcodeReference == shipment.BoxBarcodeReference))
            ?.BrandingPdfReference;
    }

    public void SetPacketTracing(int packetTracing, params string[] barcodes)
    {
        var shipment = GetShipmentByTransportationReference(barcodes);

        if (shipment is null) //TODO: Log
            return;

        shipment.PacketTracing = packetTracing;
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
        Context.Shipments.FirstOrDefault(s => s.PacketTracing == packetTracing); //TODO: Single hat Fehler ergaben wegen doppelten Eintrag??

    public ICollection<Shipment> GetShipmentsByPacketTracing(List<int> tracedPackets) =>
        Context.Shipments.Where(s => tracedPackets.Contains(s.PacketTracing)).ToList();

    /// <summary>
    /// Get all shipments which have not yet reached their destination
    /// </summary>
    /// <returns></returns>
    public ICollection<Shipment> GetRunningShipments() => Context.Shipments.Where(s => s.DestinationReachedAt is null).ToList();

    public void DestinationReached(int shipmentId)
    {
        var shipment = GetShipment(shipmentId);

        if (shipment != null)
            shipment.DestinationReachedAt = DateTime.Now;
    }

    public void UpdateShipments(object? sender, UpdateShipmentEventArgs shipments) =>
        UpdateShipments(shipments.UpdatedShipments?.ToArray());

    public void NewShipments(object? sender, NewShipmentEventArgs shipments) =>
        AddShipments(shipments.NewShipments);

    public void UpdateConfiguration(object? sender, UpdateConfigurationEventArgs configs) =>
        ConfigService.UpdateConfigs(configs.ServiceConfiguration);

    public Shipment? LogShipment(Sector sector, IList<string>? barcodes)
    {
        if (barcodes == null || !barcodes.Any())
        {
            _logger.LogWarning("{0}: Empty Barcodes", sector);
            return null;
        }

        var shipment = GetShipmentByTransportationReference(barcodes.ToArray());
        
        if (shipment == null)
            _logger.LogWarning("{0}: Unknown shipment detected. Barcodes: {1}", sector, string.Join(", ", barcodes));
        else
            _logger.LogInformation("{0}: Shipment: {1}", sector, shipment);

        return shipment;
    }
}