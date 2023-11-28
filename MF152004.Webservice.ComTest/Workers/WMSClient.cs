using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text;
using MF152004.Webservice.ComTest.Data;
using System.Linq;
using System.Threading;
using MF152004.Models.Main;
using MF152004.Models.Values.Types;
using BlueApps.MaterialFlow.Common.Models;
using System.Timers;

namespace MF152004.Webservice.ComTest.Workers
{
    public class WMSClient : IHostedService
    {
        public Dictionary<string, string>? Headers { get; set; }

        private readonly ILogger<WMSClient> _logger;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public WMSClient(ILogger<WMSClient> logger, IConfiguration configuration, IServiceProvider service)
        {
            _logger = logger;
            _configuration = configuration;
            
            var scope = service.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RunWMSClient(cancellationToken);
            _logger.LogInformation("The wms client has been started");
            return Task.CompletedTask;
        }

        private void RunWMSClient(CancellationToken cancellationToken)
        {
            string baseUrl = "";

            if (_configuration != null)
            {
                if (!string.IsNullOrEmpty(_configuration["API_Key"]))
                {
                    AddHeader("x-api-key", _configuration["API_KEY"] ?? "");
                    _logger.LogInformation("Headers has been added"); 
                }

                if (_configuration["WMSEndpoint"] != null)
                {
                    baseUrl = _configuration["WMSEndpoint"] ?? "";
                }
                else
                {
                    _logger.LogWarning("The WMSEndpoint must be defined!");
                    return;
                }                    
            }

            SendShipments(baseUrl, cancellationToken);
            SendScans(baseUrl, cancellationToken);
        }

        private async void SendShipments(string baseUrl, CancellationToken cancellationToken)
        {
            Random randomTime = new Random();
            Random randomPick = new Random();

            while (true)
            {
                await Task.Delay(randomTime.Next(5000, 15000), cancellationToken);

                if (_context.Shipments.Count() > 0)
                {
                    var count = _context.Shipments.Count();
                    var shipment = _context.Shipments.ToList().ElementAt(randomPick.Next(0, count - 1));

                    shipment.Message = "Has gone through the MFService";
                    shipment.LeftSealerAt = DateTime.Now.AddMinutes(-3);
                    shipment.BoxBrandedAt_1 = DateTime.Now.AddMinutes(-2);
                    shipment.BoxBrandedAt_2 = DateTime.Now.AddMinutes(-1).AddSeconds(-23);
                    shipment.LabelPrintedAt = DateTime.Now;

                    var time = TimeOnly.FromDateTime(DateTime.Now).ToLongTimeString();

                    try
                    {
                        await PatchAsync($"{baseUrl}shipments/budde/{shipment.Id}", shipment);
                        _logger.LogInformation($"A shipment {shipment} has been patched at {time}");
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception.Message);
                    }
                }
                else
                {
                    _logger.LogInformation("No entities in DB");
                }
            }
        }

        private async void SendScans(string baseUrl, CancellationToken cancellationToken)
        {
            Random randomStuff = new();            

            while (true)
            {
                await Task.Delay(randomStuff.Next(5000, 15000), cancellationToken);

                var scan = new Scan()
                {
                    ScanType = ScanType.successful_scan,
                    ShipmentId = randomStuff.Next(100, 9000),
                    Weight = randomStuff.Next(4000, 35500),
                    ScanTime = DateTime.Now,
                };

                var time = TimeOnly.FromDateTime(DateTime.Now).ToLongTimeString();

                try
                {
                    await PostAsync($"{baseUrl}shipments/scan", scan);
                    _logger.LogInformation($"A scan {scan} has been posted at {time}");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.Message);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("The wms client has been stopped");
            return Task.CompletedTask;
        }

        #region http

        private void AddHeader(string key, string value)
        {
            if (Headers is null)
                Headers = new Dictionary<string, string>();

            Headers.Add(key, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <exception cref="HttpRequestException"></exception>
        private async Task PostAsync<T>(string url, T data)
        {
            var jsonData = GetJsonData(data);

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    await Send(url, jsonData, HttpMethod.Post);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogError($"An error occurs in the POST-method. The statuscode is {exception.StatusCode}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <exception cref="HttpRequestException"></exception>
        private async Task PatchAsync<T>(string url, T data)
        {
            var jsonData = GetJsonData(data);

            if (!string.IsNullOrEmpty(jsonData))
            {
                try
                {
                    await Send(url, jsonData, HttpMethod.Patch);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogError($"An error occurs in the PATH-method. The statuscode is {exception.StatusCode}");
                    throw;
                }
            }
        }

        private async Task Send(string url, string data, HttpMethod httpMethod)
        {
            using var client = new HttpClient();

            CreateHeader(client);

            var request = new HttpRequestMessage(httpMethod, url);

            request.Content = new StringContent(data, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        private void CreateHeader(HttpClient client)
        {
            if (client is null) return;

            if (Headers != null && Headers.Count > 0)
            {
                foreach (var kp in Headers)
                {
                    client.DefaultRequestHeaders.Add(kp.Key, kp.Value);
                }
            }
        }

        private string GetJsonData<T>(T data)
        {
            if (data is null)
                return string.Empty;

            try
            {
                var jsonData = JsonSerializer.Serialize(data, typeof(T));

                return jsonData;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
                return string.Empty;
            }
        }

        #endregion
    }
}
