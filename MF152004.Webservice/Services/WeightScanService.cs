using MF152004.Models.Main;
using MF152004.Webservice.Data;

namespace MF152004.Webservice.Services;

public class WeightScanService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WeightScanService> _logger;

    public WeightScanService(ApplicationDbContext context, ILogger<WeightScanService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddWeightScan(Scan? scan)
    {
        if (scan is null)
        {
            _logger.LogWarning("The scan is null");
            return;
        }

        _context.WeightScans.Add(scan);
        await _context.SaveChangesAsync();                
    }
}