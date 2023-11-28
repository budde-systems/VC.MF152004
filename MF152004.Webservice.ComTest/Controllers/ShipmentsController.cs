using MF152004.Models.Main;
using MF152004.Webservice.ComTest.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MF152004.Webservice.ComTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShipmentsController : ControllerBase
    {
        private readonly ILogger<ShipmentsController> _logger;
        private readonly AppDbContext _context;

        public ShipmentsController(ILogger<ShipmentsController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }
            

        [HttpPost]
        public IActionResult PostShipment(Shipment shipment) //required
        {
            var result = ValidateShipment(shipment);

            if (!result.Item1)
                return BadRequest(result.Item2);

            if (ShipmentExists(shipment.Id))
                return Conflict($"The shipment {shipment} already exists. Use Http-PUT to update entity."); //409

            shipment.ReceivedAt = DateTime.Now;

            _context.Shipments.Add(shipment);
            _context.SaveChanges();

            string json = JsonSerializer.Serialize(shipment, new JsonSerializerOptions() { WriteIndented = true});
            _logger.LogInformation(json);

            return Ok();
        }

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

        private bool ShipmentExists(int id)
        {
            return (_context.Shipments?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpPut("{id}")]
        public IActionResult PutShipment(int id, Shipment shipment) //required
        {
            if (id != shipment.Id)
            {
                return BadRequest("The parameter ID and the shipment ID are different");
            }

            _context.Entry(shipment).State = EntityState.Modified;

            try
            {
                _context.SaveChanges();

                string json = JsonSerializer.Serialize(shipment, new JsonSerializerOptions() { WriteIndented = true});
                _logger.LogInformation(json);

                return Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShipmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return StatusCode(500);
                }
            }
        }
    }
}
