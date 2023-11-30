using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MF152004.Models.Main;
using MF152004.Webservice.Data;
using MF152004.Webservice.Services;
using MF152004.Webservice.Filters;

namespace MF152004.Webservice.Controller;

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

        if (result.Item1 == false)
        {
            _logger.LogWarning($"Not passed validation for shipment: {shipment}");
            return BadRequest(result.Item2);
        }

        if (await ShipmentExists(shipment.Id))
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

    private async Task<bool> ShipmentExists(int id) => await _context.Shipments.AnyAsync(s => s.Id == id);

    private (bool, string) ValidateShipment(Shipment shipment)
    {
        return shipment.Id <= 0 ? new(false, "The id of shipment cannot be zero or less.")
            : string.IsNullOrEmpty(shipment.ClientReference) ? new(false, GetDefaultMessage(nameof(shipment.ClientReference)))
            : string.IsNullOrEmpty(shipment.BoxBarcodeReference) ? new(false, GetDefaultMessage(nameof(shipment.BoxBarcodeReference)))
            : string.IsNullOrEmpty(shipment.TransportationReference) ? new(false, GetDefaultMessage(nameof(shipment.TransportationReference)))
            : string.IsNullOrEmpty(shipment.Carrier) ? new(false, GetDefaultMessage(nameof(shipment.Carrier)))
            : string.IsNullOrEmpty(shipment.Country) ? new(false, GetDefaultMessage(nameof(shipment.Country)))
            : string.IsNullOrEmpty(shipment.Status) ? new(false, GetDefaultMessage(nameof(shipment.Status)))
            : shipment.Weight <= 0 ? new(false, $"The value of the field {nameof(shipment.Weight)} cannot be zero or less.")
            : string.IsNullOrEmpty(shipment.TrackingCode) ? new(false, GetDefaultMessage(nameof(shipment.TrackingCode)))
            : new(true, string.Empty);
    }

    private string GetDefaultMessage(string nameOfField) => $"The {nameOfField} field cannot be empty or null.";
}