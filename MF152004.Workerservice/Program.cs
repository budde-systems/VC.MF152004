using BlueApps.MaterialFlow.Common.Connection.Broker;
using BlueApps.MaterialFlow.Common.Connection.Client;
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

            Log.Logger.Information("Workerservice is starting up...");
            Log.Logger.Information("================================================");
            
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services)=>
                {
                    services.AddWindowsService(options => options.ServiceName = "BlueApps_MaterialFlow");                        
                    services.AddScoped<MqttBroker>();
                    services.AddScoped<DestinationService>();
                    services.AddSingleton<ContextService>();
                    services.AddSingleton<MqttClient>();
                    services.AddSingleton<MaterialFlowMng>();                        
                    services.AddHostedService<Worker>();
                }).UseSerilog(Log.Logger)
                .Build();

            host.Run();
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Logger.Error(e.ExceptionObject as Exception, $"AppDomain Unhandled exception ({e.IsTerminating})");
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