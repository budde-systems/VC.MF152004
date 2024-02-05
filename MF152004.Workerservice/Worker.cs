using BlueApps.MaterialFlow.Common.Connection.Broker;
using MF152004.Workerservice.Logic;

namespace MF152004.Workerservice;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly MaterialFlowMng _materialFlowManager;
    private readonly IServiceProvider _services;

    public Worker(ILogger<Worker> logger, MaterialFlowMng materialFlowManager, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
        _materialFlowManager = materialFlowManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Workerservice is starting up...");
            _logger.LogInformation("================================================");

            var scope = _services.CreateScope();
            var broker = scope.ServiceProvider.GetRequiredService<MqttBroker>();

            await broker.RunBrokerAsync();

            _ = Task.Factory.StartNew(() => _materialFlowManager.Run(stoppingToken), TaskCreationOptions.LongRunning); // Running MaterialFlow in another thread

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                _logger.LogInformation("Heartbeat: Workerservice is running at: {time}", DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
        }
    }
}