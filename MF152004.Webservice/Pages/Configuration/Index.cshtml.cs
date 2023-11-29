using BlueApps.MaterialFlow.Common.Models;
using MF152004.Webservice.Data;
using MF152004.Webservice.Data.PageData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MF152004.Webservice.Pages.Configuration;

public class IndexModel : PageModel
{
    public ConfigurationDTO ConfigurationData { get; set; }

    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;

        GetConfigurationData();
    }

    private async Task GetConfigurationData()
    {
        ConfigurationData = new ConfigurationDTO()
        {
            Carriers = await _context.Carriers
                .GroupBy(_ => _.Name)
                .Select(_ => _.First())
                .ToListAsync(),

            Countries = await _context.Countries
                .GroupBy(_ => _.Name)
                .Select(_ => _.First())
                .ToListAsync(),

            ClientReferences = await _context.ClientReferences
                .GroupBy(_ => _.Name)
                .Select(_ => _.First())
                .ToListAsync()
        };
    }

    public void OnGet()
    {
            
    }

    public async Task<IActionResult> OnGetConfigurationDataAsync()
    {
        await GetConfigurationData();

        return Partial("_ConfigurationDataPartial", ConfigurationData);
    }

    public async Task<IActionResult> OnPostNewCarrierAsync(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var destinations = await _context.Destinations
                .Include(_ => _.Carriers)
                .ToListAsync();

            foreach (var destination in destinations)
            {
                destination.Carriers ??= new();

                if (!destination.Carriers.Any(_ => _.Name.ToLower() == name.ToLower()))
                {
                    destination.Carriers.Add(new Carrier
                    {
                        Name = name,
                        Active = false
                    });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"A new carrier: {name} has been inserted to all available destinations DB");
        }

        return RedirectToPage("/configuration/index");
    }

    public async Task<IActionResult> OnPostRemoveCarrierAsync(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var destinations = await _context.Destinations
                .Include(_ => _.Carriers)
                .ToListAsync();

            foreach (var destination in destinations)
            {
                if (destination.Carriers != null)
                {
                    if (destination.Carriers.Any(_ => _.Name.ToLower() == name.ToLower()))
                    {
                        _context.RemoveRange(destination.Carriers.Single(_ => _.Name == name));
                    } 
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"The carrier: {name} has been removed from all available destinations in DB");

            return RedirectToPage("/configuration/index");
        }

        return BadRequest();
    }

    public async Task<IActionResult> OnPostNewCountryAsync(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var destinations = await _context.Destinations
                .Include(_ => _.Countries)
                .ToListAsync();

            foreach (var destination in destinations)
            {
                destination.Countries ??= new();

                if (!destination.Countries.Any(_ => _.Name.ToLower() == name.ToLower()))
                {
                    destination.Countries.Add(new Country
                    {
                        Name = name,
                        Active = false
                    });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"A new country: {name} has been inserted to all available destinations DB");
        }

        return RedirectToPage("/configuration/index");
    }

    public async Task<IActionResult> OnPostRemoveCountryAsync(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var destinations = await _context.Destinations
                .Include(_ => _.Countries)
                .ToListAsync();

            foreach (var destination in destinations)
            {
                if (destination.Countries != null)
                {
                    if (destination.Countries.Any(_ => _.Name.ToLower() == name.ToLower()))
                    {
                        _context.RemoveRange(destination.Countries.Single(_ => _.Name == name));
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"The country: {name} has been removed from all available destinations in DB");

            return RedirectToPage("/configuration/index");
        }

        return BadRequest();
    }

    public async Task<IActionResult> OnPostNewClientIdAsync(string clientId)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            var destinations = await _context.Destinations
                .Include(_ => _.ClientReferences)
                .ToListAsync();

            foreach (var destination in destinations)
            {
                destination.ClientReferences ??= new();

                if (!destination.ClientReferences.Any(_ => _.Name.ToLower() == clientId.ToLower()))
                {
                    destination.ClientReferences.Add(new ClientReference
                    {
                        Name = clientId,
                        Active = false
                    });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"A new client reference: {clientId} has been inserted " +
                                   $"to all available destinations DB");
        }

        return RedirectToPage("/configuration/index");
    }

    public async Task<IActionResult> OnPostRemoveClientIdAsync(string clientId)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            var destinations = await _context.Destinations
                .Include(_ => _.ClientReferences)
                .ToListAsync();

            foreach (var destination in destinations)
            {
                if (destination.ClientReferences != null)
                {
                    if (destination.ClientReferences.Any(_ => _.Name.ToLower() == clientId.ToLower()))
                    {
                        _context.RemoveRange(destination.ClientReferences.Single(_ => _.Name == clientId));
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"The client reference: {clientId} has been removed from all " +
                                   $"available destinations in DB");

            return RedirectToPage("/configuration/index");
        }

        return BadRequest();
    }
}