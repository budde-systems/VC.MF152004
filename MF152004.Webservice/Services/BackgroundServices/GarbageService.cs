using MF152004.Common.Data;
using MF152004.Webservice.Data;
using MF152004.Webservice.Services.BackgroundServices.BackgroundServicesSettings;
using Microsoft.EntityFrameworkCore;

namespace MF152004.Webservice.Services.BackgroundServices;

public class GarbageService : BackgroundService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GarbageService> _logger;
    private readonly GarbageServiceSettings _settings;

    private DateTime _finishedToDay = DateTime.Now.AddDays(-1);

    public GarbageService(ILogger<GarbageService> logger, IConfiguration configuration, IServiceProvider service)
    {
        var scope = service.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _logger = logger;

        try
        {
            _settings = configuration.GetRequiredSection("garbageservice").Get<GarbageServiceSettings>() ??
                        GetDefaultSettings();
        }
        catch (Exception exception)
        {
            _logger.LogError("Error at garbage service settings. Default values will be used. Exception:\n" +
                             $"{exception}");
            _settings = GetDefaultSettings();
        }
    }

    private GarbageServiceSettings GetDefaultSettings() => new() //default values
    {
        Period = 30,
        ExecuteTime = new TimeOnly(23, 30),
        KeepDeliveredZplFileDays = 21,
        KeepOldZplFilesDays = 60,
        KeepShipmentDays = 400
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (!FinishedToDay() && TimeOnly.FromDateTime(DateTime.Now) >= _settings.ExecuteTime)
            {
                await RemoveZplFiles();
                await RemoveOldShipments();
                _finishedToDay = DateTime.Now.Date;
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.Period), stoppingToken);
        }
    }

    private bool FinishedToDay() => _finishedToDay.Date == DateTime.Now.Date;

    private async Task RemoveZplFiles()
    {
        var shippedShipments = await _context.Shipments
            .Where(_ => _.DestinationReachedAt != null)
            .ToListAsync();

        var removedFiles = FileManager
            .RemoveZplFiles(_settings.KeepDeliveredZplFileDays, shippedShipments.Select(_ => _.Id.ToString()).ToArray());                

        var oldRemovedFiles = FileManager.RemoveZplFiles(_settings.KeepOldZplFilesDays);

        if (removedFiles.Any() || oldRemovedFiles.Any())
        {
            var allDeletedFiles = new List<string>(removedFiles.Count + oldRemovedFiles.Count);
            allDeletedFiles.AddRange(removedFiles);
            allDeletedFiles.AddRange(oldRemovedFiles);

            _logger.LogInformation($"Files are deleted:\n{string.Join("\n", allDeletedFiles)}");
        }
        else
            _logger.LogInformation("No files will be deleted");

    }

    private async Task RemoveOldShipments()
    {
        try
        {
            var removedShipments = await _context.Shipments
                .Where(_ => _.DestinationReachedAt != null && _.DestinationReachedAt < DateTime.Now.AddDays(-_settings.KeepShipmentDays))
                .ExecuteDeleteAsync();

            _logger.LogInformation($"{removedShipments} old shipments has been removed from DB");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.ToString());
        }
    }
}