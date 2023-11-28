using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Connection.Client.Http;
using MF152004.Common.Connection.Hubs;
using MF152004.Webservice.Data;
using MF152004.Webservice.Services;
using MF152004.Webservice.Services.BackgroundServices;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using System.Diagnostics;

namespace MF152004.Webservice
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureLogger(builder);

            var connectionString = builder.Configuration.GetConnectionString("MF152004Connection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddScoped<ConfigurationService>();
            builder.Services.AddScoped<ShipmentService>();
            builder.Services.AddScoped<WMS_Client>();
            builder.Services.AddScoped<DestinationService>();
            builder.Services.AddScoped<WeightScanService>();
            builder.Services.AddSingleton<MqttClient>();
            builder.Services.AddSingleton<GeneralPacketService>();
            builder.Services.AddSingleton<MessageDistributorService>();
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddHostedService<GarbageService>();
            //TODO: AddScoped<ConfigurationService> => IConfiguration in Topics und Keys von dort aus bearbeite / prüfen etc. ??

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            MigrateDatabase(app.Services);

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseHttpLogging();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();
            app.MapHub<WorkerWebHub>("/workerWebCom");
            app.MapControllers();

            app.Run();
        }

        private static void ConfigureLogger(WebApplicationBuilder builder)
        {
            //appsettings is not working because of an issue
            //Log.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(builder.Configuration)
            //    .CreateLogger();

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware"))
                .WriteTo.File("C:/BlueApplications/MF152004/Logs/web/mf152004_web_http_.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} [{Level:u3}] ({SourceContext}) {Message:lj}{Exception}{NewLine}"))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource("BlueApps.MaterialFlow.Common.Connection.Client.Http"))
                .WriteTo.File("C:/BlueApplications/MF152004/Logs/web/mf152004_web_wms_.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} [{Level:u3}] ({SourceContext}) {Message:lj}{Exception}{NewLine}"))
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.HttpLogging"))
                .Filter.ByExcluding(Matching.FromSource("BlueApps.MaterialFlow.Common.Connection.Client.Http"))
                .WriteTo.File("C:/BlueApplications/MF152004/Logs/web/mf152004_web_.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 90, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {CorrelationId} [{Level:u3}] ({SourceContext}) {Message:lj}{Exception}{NewLine}"))
            .CreateLogger();

            builder.Services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = HttpLoggingFields.All;
                logging.MediaTypeOptions.AddText("application/javascript");
                logging.RequestBodyLogLimit = 4096;
                logging.ResponseBodyLogLimit = 4096;
            });

            builder.Logging.AddSerilog(Log.Logger);
        }

        private static void MigrateDatabase(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var srvs = scope.ServiceProvider;
            var context = srvs.GetRequiredService<ApplicationDbContext>();

            context.Database.Migrate();
        }
    }
}