using BlueApps.MaterialFlow.Common.Connection.Broker;
using BlueApps.MaterialFlow.Common.Connection.Client;
using MF152004.Common.Connection.Clients;
using MF152004.Models.Settings.BrandPrinter;
using MF152004.Workerservice.Logic;
using MF152004.Workerservice.Services;
using Serilog;
using Serilog.Events;

namespace MF152004.Workerservice;

public class Program
{
    public static void Main(string[] args)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            ConfigureLogger();

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services)=>
                {
                    services.Configure<BrandPrinterSettingsFront>(hostContext.Configuration
                        .GetSection("brand_printer_config_front"));
                    services.Configure<BrandPrinterSettingsBack>(hostContext.Configuration
                        .GetSection("brand_printer_config_back"));

                    services.AddWindowsService(options => options.ServiceName = "BlueApps_MaterialFlow");                        
                    services.AddScoped<MqttBroker>();
                    services.AddScoped<DestinationService>();
                    services.AddSingleton<ContextService>();
                    services.AddSingleton<MqttClient>();
                    services.AddSingleton<BrandingPrinterClient>();
                    services.AddSingleton<MaterialFlowMng>();                        
                    services.AddHostedService<Worker>();
                }).UseSerilog(Log.Logger)
                .Build();

            host.Run();
        }
    }

    private static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("C:/BlueApplications/MF152004/Logs/worker/mf152004_worker_.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 90, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} [{Level:u3}] ({SourceContext}) {Message:lj}{Exception}{NewLine}")
            .CreateLogger();
    }
}