using BlueApps.MaterialFlow.Common.Models.Configurations;
using MF152004.Webservice.Filters;
using MF152004.Webservice.Services;
using Microsoft.AspNetCore.Mvc;

namespace MF152004.Webservice.Controller;

[Route("api/[controller]")]
[ApiController]
[KeyAuthorization]
public class ConfigurationController : ControllerBase
{
    private readonly ConfigurationService _configurationService;
    private readonly ILogger<ConfigurationController> _logger;
    private readonly MessageDistributorService _messageDistributorService;

    public ConfigurationController(ConfigurationService configurationService, ILogger<ConfigurationController> logger,
        MessageDistributorService messageDistributorService)
    {
        _logger = logger;
        _configurationService = configurationService;
        _messageDistributorService = messageDistributorService;

    }

    // POST: api/Shipments
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<IActionResult> PostConfiguration(Configuration[] configuration) //required
    {
        try
        {
            await _configurationService.SetConfiguration(configuration);

            DistributeConfiguration();

            return Ok(configuration);
        }
        catch (Exception exception)
        {

            return BadRequest(exception.Message);
        }
    }

    private async void DistributeConfiguration()
    {
        var configs = await _configurationService.GetActiveConfiguration();
        _messageDistributorService.SendNewServiceConfiguration(configs);
    }

    //[HttpGet]
    //public async Task<ActionResult<Configuration>> GetConfiguration()
    //{
    //    return new Configuration() { BrandingPdfConfigs = new Configuration<BrandingPdf[]> { new BrandingPdf() {  } } }
    //}
}