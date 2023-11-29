using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Models.Values.Types;
using MF152004.Webservice.Common;
using MF152004.Webservice.Services;

namespace MF152004.Webservice;

public class Worker : BackgroundService
{
    private readonly MqttClient _client;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly MessageDistributorService _messageDistributorService;

    private CancellationToken _cancellationToken;

    public Worker(MqttClient client, ILogger<Worker> logger, IConfiguration configuration, 
        MessageDistributorService messageDistributorService, GeneralPacketService generalPaketService)
    {
        _configuration = configuration;
        _client = client;
        _logger = logger;
        _messageDistributorService = messageDistributorService;
            
        SetTopics();
        SetEndpoints();
    }

    private void SetTopics()
    {
        CommonData.Topics = new()
        {
            { 
                TopicType.PLC_Workerservice, 
                _configuration["PLC_To_Workerservice"] ?? "MaterialFlow/plc/workerservice" 
            },
            { 
                TopicType.WebService_Workerservice, 
                _configuration["Webservice_To_Workerservice"] ?? "MaterialFlow/webservice/workerservice" 
            },
            { 
                TopicType.WebService_Workerservice_Config, 
                _configuration["Webservice_To_Workerservice_Config"] ?? "MaterialFlow/webservice/workerservice/config" 
            },
            { 
                TopicType.Workerservice_Webservice, 
                _configuration["Workerservice_To_Webservice"] ?? "MaterialFlow/workerservice/webservice" 
            },
            { 
                TopicType.Workerservice_PLC, 
                _configuration["Workerservice_To_PLC"] ?? "MaterialFlow/workerservice/plc" 
            },
            { 
                TopicType.Workerservice_Webservice_Config, 
                _configuration["Workerservice_To_Webservice_Config"] ?? "MaterialFlow/workerservice/webservice/config" 
            },
            { 
                TopicType.Workerservice_Webservice_Destination, 
                _configuration["Workerservice_To_Webservice_Destination"] ?? "MaterialFlow/workerservice/webservice/destination" 
            },
            { 
                TopicType.Webservice_Workerservice_Destination, 
                _configuration["Webservice_To_Workerservice_Destination"] ?? "MaterialFlow/webservice/workerservice/destination" 
            },
            {
                TopicType.Workerservice_Webservice_General,
                _configuration["Workerservice_To_Webservice_General"] ?? "MaterialFlow/workerservice/webservice/general"
            },
            {
                TopicType.Webservice_Workerservice_General,
                _configuration["Webservice_To_Workerservice_General"] ?? "MaterialFlow/webservice/workerservice/general"
            },
            {
                TopicType.Workerservice_Webservice_WeightScan,
                _configuration["Workerservice_To_Webservice_WeightScan"] ?? "MaterialFlow/workerservice/webservice/weightscan"
            },
            {
                TopicType.Webservice_Workerservice_WeightScan,
                _configuration["Webservice_To_Workerservice_WeightScan"] ?? "MaterialFlow/webservice/workerservice/weightscan"
            }
        };
    }

    private void SetEndpoints()
    {
        CommonData.Endpoints = new()
        {
            { API_Endpoint.PATCH_Schipment, _configuration["URL_PatchShipments"] ?? "" },
            { API_Endpoint.POST_ScaleScan, _configuration["URL_PostScaleScan"] ?? "" },
            { API_Endpoint.GET_Labels, _configuration["URL_GetLabels"] ?? "" }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancellationToken = stoppingToken;
        _logger.LogInformation("Worker has been started successfully");

        await PrepareClient();

        await DoWork();
    }

    private async Task PrepareClient()
    {
        _client.AddTopics(CommonData.Topics[TopicType.Workerservice_Webservice], 
            CommonData.Topics[TopicType.Workerservice_Webservice_Config],
            CommonData.Topics[TopicType.Workerservice_Webservice_Destination],
            CommonData.Topics[TopicType.Workerservice_Webservice_General],
            CommonData.Topics[TopicType.Workerservice_Webservice_WeightScan]);

        await ConnectClient();

        _client.OnReceivingMessage += _messageDistributorService.DistributeIncomingMessages;
        _client.ClientDisconnected += OnClientDisconnected;
    }

    private async void OnClientDisconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning("Connection to broker has been lost.");
        await ConnectClient();
    }

    private async Task ConnectClient()
    {
        while (!_client.IsConnected)
        {
            await _client.Connect(_cancellationToken);

            if (!_client.IsConnected)
            {
                _logger.LogWarning("Couldn't connect to broker. Next connection establishment in 60 secs.");

                await Task.Delay(TimeSpan.FromSeconds(60), _cancellationToken);
            }
        }
    }

    private async Task DoWork()
    {
            
    }

}