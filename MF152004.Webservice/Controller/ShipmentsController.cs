using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MF152004.Models.Main;
using MF152004.Webservice.Data;
using MF152004.Webservice.Services;
using MF152004.Webservice.Filters;

namespace MF152004.Webservice.Controller
{
    [Route("api/shipments")]
    [ApiController]
    [KeyAuthorization]
    public class ShipmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShipmentsController> _logger;
        private readonly MessageDistributorService _messageDistributorService;
        private readonly DestinationService _destinationService;
        private readonly ShipmentService _shipmentService;

        public ShipmentsController(ApplicationDbContext context, ILogger<ShipmentsController> logger,
            MessageDistributorService messageDistributorService, DestinationService destinationService, ShipmentService shipmentService)
        {
            _context = context;
            _logger = logger;
            _messageDistributorService = messageDistributorService;
            _destinationService = destinationService;
            _shipmentService = shipmentService;
        }

        // PUT: api/Shipments/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutShipment(int id, Shipment shipment) //required
        {
            if (shipment is null)
                return BadRequest("The shipment object is null");

            if (!await ShipmentExists(id))
                return NotFound($"The shipment with the ID {id} could not be found");

            var storedShipment = await _shipmentService.PutShipment(id, shipment);

            if (storedShipment is null)
                return StatusCode(500);

            _messageDistributorService.SendUpdatedShipments(storedShipment);
            _messageDistributorService.GetLabelsAsync(storedShipment.Id);

            return Ok();
        }

        // POST: api/Shipments
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostShipment(Shipment shipment) //required
        {
            _logger.LogInformation($"A new shipment ({shipment}) was received.");
            
            var result = ValidateShipment(shipment);

            if (_context.Shipments == null)
            {
                _logger.LogError("Entity set 'ApplicationDbContext.Shipments' is null.");
                return StatusCode(500);
            }
            else if (result.Item1 == false)
            {
                _logger.LogWarning($"Not passed validation for shipment: {shipment}");
                return BadRequest(result.Item2);
            }
            else if (await ShipmentExists(shipment.Id))
            {
                _logger.LogWarning($"Shipment {shipment} already exists.");
                return Conflict($"The shipment {shipment} already exists. Use Http-PUT to update entity."); //409
            }

            shipment.ReceivedAt = DateTime.Now;
            shipment.DestinationRouteReference = _destinationService
                .GetDestinationNames(shipment.Carrier, shipment.Country, shipment.ClientReference);

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            _messageDistributorService.GetLabelsAsync(shipment.Id);

            _messageDistributorService.SendNewShipments(shipment);

            return Ok();
        }

        private async Task<bool> ShipmentExists(int id) => await _context.Shipments.AnyAsync(_ => _.Id == id);

        private (bool, string) ValidateShipment(Shipment shipment)
        {
            if (shipment.Id <= 0)
                return new(false, "The id of shipment cannot be zero or less.");
            else if (string.IsNullOrEmpty(shipment.ClientReference))
                return new(false, GetDefaultMessage(nameof(shipment.ClientReference)));
            else if (string.IsNullOrEmpty(shipment.BoxBarcodeReference))
                return new(false, GetDefaultMessage(nameof(shipment.BoxBarcodeReference)));
            else if (string.IsNullOrEmpty(shipment.TransportationReference))
                return new(false, GetDefaultMessage(nameof(shipment.TransportationReference)));
            else if (string.IsNullOrEmpty(shipment.Carrier))
                return new(false, GetDefaultMessage(nameof(shipment.Carrier)));
            else if (string.IsNullOrEmpty(shipment.Country))
                return new(false, GetDefaultMessage(nameof(shipment.Country)));
            else if (string.IsNullOrEmpty(shipment.Status))
                return new(false, GetDefaultMessage(nameof(shipment.Status)));
            else if (shipment.Weight <= 0)
                return new(false, $"The value of the field {nameof(shipment.Weight)} cannot be zero or less.");
            else if (string.IsNullOrEmpty(shipment.TrackingCode))
                return new(false, GetDefaultMessage(nameof(shipment.TrackingCode)));

            return new(true, string.Empty);
        }

        private string GetDefaultMessage(string nameOfField) =>
            $"The {nameOfField} field cannot be empty or null.";
    }
}
