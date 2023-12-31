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
#if !SAFE_DEBUG
        await StartBroker();
#endif

        _materialFlowManager.Run(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Workerservice running at: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task StartBroker()
    {
        var serviceScope = _services.CreateScope();
        var broker = serviceScope.ServiceProvider.GetService<MqttBroker>();

        if (broker != null)
            await broker.RunBrokerAsync();
        else
            Environment.Exit(0); //TODO: logging etc.
    }
}