using MF152004.Models.Main;
using MF152004.Webservice.Data;
using Microsoft.EntityFrameworkCore;

namespace MF152004.Webservice.Services
{
    public class ShipmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShipmentService> _logger;
        private readonly DestinationService _destinationService;

        public ShipmentService(ApplicationDbContext context, ILogger<ShipmentService> logger, DestinationService destinationService)
        {
            _context = context;
            _logger = logger;
            _destinationService = destinationService;
        }

        public async Task<List<Shipment>> GetThirtyDaysShipments()
        {
            var shipments = await _context.Shipments
                .Where(_ => _.ReceivedAt != null && 
                    _.ReceivedAt.Value.Date <= DateTime.Now.Date && 
                    _.ReceivedAt.Value.Date > DateTime.Now.Date.AddDays(-14))
                .ToListAsync();

            return shipments;
        }

        /// <summary>
        /// In case of shipments is null, this function is not executed.
        /// </summary>
        /// <param name="shipments"></param>
        public async void UpdateShipments(List<Shipment>? shipments)
        {
            if (shipments is null)
            {
                _logger.LogWarning("Update can not be executed, because shipments is null");
            }
            else
            {
                try
                {
                    _context.ChangeTracker.Entries<Shipment>().ToList().ForEach(entry => entry.State = EntityState.Detached);

                    _context.Shipments.UpdateRange(shipments);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Update of the shipments was successfully performed");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.ToString());
                }
            }
        }

        /// <summary>
        /// Shipmment will be updated: weight, status, received date, left error aisle date and the transportation reference. <br/>
        /// The fields label printed at and message will be removed. A new destination will be set.
        /// </summary>
        /// <param name="shipmentId"></param>
        /// <param name="shipment"></param>
        /// <returns>The updated shipment or null if something went wrong.</returns>
        public async Task<Shipment?> PutShipment(int shipmentId, Shipment shipment)
        {
            if (shipment is null)
            {
                _logger.LogWarning("Update can not be executed, because shipment is null");
                return null;
            }
            else
            {
                var storedShipment = await _context.Shipments.SingleOrDefaultAsync(_ => _.Id == shipmentId);

                if (storedShipment is null)
                    return null;

                storedShipment.Weight = shipment.Weight;
                storedShipment.Status = shipment.Status;
                storedShipment.ReceivedAt = DateTime.Now;
                storedShipment.LeftErrorAisleAt = shipment.LeftErrorAisleAt;
                storedShipment.LabelPrintedAt = null;
                storedShipment.Message = string.Empty;

                if (shipment.TransportationReference != null)
                {
                    _logger.LogInformation($"The transportation reference of shipment {storedShipment} " +
                        $"has been changed to {shipment.TransportationReference}");
                    storedShipment.TransportationReference = shipment.TransportationReference;
                }

                if (shipment.TrackingCode != null && storedShipment.TrackingCode != shipment.TrackingCode)
                {
                    _logger.LogInformation($"The trackingcode of shipment {storedShipment} " +
                        $"has been changed to {shipment.TrackingCode}");

                    storedShipment.TrackingCode = shipment.TrackingCode;
                }

                storedShipment.DestinationRouteReference = _destinationService
                    .GetDestinationNames(storedShipment.Carrier, storedShipment.Country, storedShipment.ClientReference);

                _logger.LogInformation("Destination updated in {0}: {1} (2)", storedShipment, storedShipment.DestinationRouteReference);

                _logger.LogInformation($"A shipment ({storedShipment}) will be updated");

                _context.Entry(storedShipment).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return storedShipment;
            }
        }

        /// <summary>
        /// Shipments is also be updated in db
        /// </summary>
        /// <param name="requestedShipments"></param>
        /// <returns>Shipments with updated (except those with fault destination) destination-reference</returns>
        internal async Task<List<Shipment>> GetShipments(List<int> requestedShipments)
        {
            var shipments = await _context.Shipments
                .Where(s => requestedShipments.Contains(s.Id))
                .ToListAsync();
            //second condition because ef core does not support this mapping???!
            var reducedShipments = shipments.Where(s => s.DestinationRouteReference != null && s.DestinationRouteReference != _destinationService
                                .GetDestinationNames(s.Carrier, s.Country, s.ClientReference)).ToList();

            try
            {
                reducedShipments.ForEach(s =>
                {
                    s.ReceivedAt = DateTime.Now;

                    if (s.DestinationRouteReference != _destinationService.GetFaultDestination().Name) //fault route should not be changed
                    {
                        s.DestinationRouteReference = _destinationService
                                .GetDestinationNames(s.Carrier, s.Country, s.ClientReference);
                        _logger.LogInformation("Destination updated in {0}: {1} (3)", s, s.DestinationRouteReference);
                    }
                });

                UpdateShipments(reducedShipments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            return reducedShipments;
        }
    }
}
