using BlueApps.MaterialFlow.Common.Models;
using MF152004.Webservice.Data;
using MF152004.Webservice.Data.PageData;
using MF152004.Webservice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MF152004.Webservice.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly MessageDistributorService _messageDistributorService;

        public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context, 
            MessageDistributorService messageDistributorService)
        {
            _logger = logger;
            _context = context;
            _messageDistributorService = messageDistributorService;
        }

        public void OnGet()
        {

        }

        public async Task<PartialViewResult> OnGetGateInformations(string gate)
        {
            var destination = await _context.Destinations
                .Include(_ => _.Carriers)
                .Include(_ => _.Countries)
                .Include(_ => _.ClientReferences)
                .FirstOrDefaultAsync(_ => _.UI_Id == gate);

            if (destination == null)
            {
                _logger.LogWarning("Destination is null. Partialview can't be created.");
                return Partial("_GateConfigurationPartial", new DestinationDTO(new()));
            }

            var destinationDTO = new DestinationDTO(destination);

            return Partial("_GateConfigurationPartial", (destinationDTO));
        }

        public async Task<IActionResult> OnPostGateProperties(int? gateId, int? carrierId, int[]? countryIds, int[]? clientIds)
        {
            if (gateId is null ||  carrierId is null || countryIds is null || clientIds is null)
            {
                _logger.LogWarning("Some of the parameters were null");
                return BadRequest();
            }

            var destination = await _context.Destinations
                .Include(_ => _.Carriers)
                .Include(_ => _.Countries)
                .Include(_ => _.ClientReferences)
                .SingleOrDefaultAsync(_ => _.Id == gateId);

            if (destination == null)
            {
                string msg = "Gate / Destination could not be found";
                _logger.LogWarning(msg);
                return NotFound(msg);
            }

            destination.Carriers?.ForEach(_ => _.Active = false);
            destination.Carriers.SingleOrDefault(_ => _.Id == carrierId).Active = true;

            destination.Countries?
                .ForEach(c => c.Active = countryIds.Contains(c.Id) ? c.Active = true : c.Active = false);

            destination.ClientReferences?
                .ForEach(c => c.Active = clientIds.Contains(c.Id) ? c.Active = true : c.Active = false);

            await _context.SaveChangesAsync();
            _messageDistributorService.SendUpdatedDestinations(destination);

            return RedirectToPage("index");
        }
    }
}