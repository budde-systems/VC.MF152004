using BlueApps.MaterialFlow.Common.Models.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MF152004.Webservice.ComTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly ILogger<ConfigurationController> _logger;

        public ConfigurationController(ILogger<ConfigurationController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostConfiguration(Configuration[] configuration) //required
        {
            try
            {
                string json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation(json);

                return Ok();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
                return StatusCode(500);
            }
        }
    }
}
