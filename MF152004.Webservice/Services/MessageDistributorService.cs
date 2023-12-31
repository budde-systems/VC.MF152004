﻿using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Connection.Client.Http;
using BlueApps.MaterialFlow.Common.Connection.PacketHelper;
using BlueApps.MaterialFlow.Common.Connection.Packets;
using BlueApps.MaterialFlow.Common.Connection.Packets.Events;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Common.Connection.Packets.PacketHelpers;
using MF152004.Common.Data;
using MF152004.Models.Configurations;
using MF152004.Models.Connection.Packets.HubPacket;
using MF152004.Models.Main;
using MF152004.Models.Values.Types;
using MF152004.Webservice.Common;
using Microsoft.AspNetCore.SignalR.Client;

namespace MF152004.Webservice.Services;

public class MessageDistributorService : MessageDistributor
{
    public override event EventHandler<BarcodeScanEventArgs>? BarcodeScanned;
    public override event EventHandler<WeightScanEventArgs>? WeightScanned;
    public override event EventHandler<UnsubscribedPacketEventArgs>? UnsubscribedPacket;
    public override event EventHandler<ErrorcodeEventArgs>? ErrorCodeTriggered;

    public event EventHandler<GeneralPacketEventArgs>? GeneralPacketReceived;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageDistributorService> _logger;
    private readonly MqttClient _client;
    private readonly ConfigurationService _configurationService;
    private readonly WMS_Client _wmsClient;
    private readonly DestinationService _destinationService;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly HubConnection? _hubConnection;

    public MessageDistributorService(IServiceProvider serviceProvider, IConfiguration configuration) : base(new List<MessagePacketHelper> 
    { 
        new ShipmentPacketHelper(configuration["Workerservice_To_Webservice"] ?? "", ""),
        new ConfigurationPacketHelper(configuration["Workerservice_To_Webservice_Config"] ?? "", ""),
        new DestinationPacketHelper(configuration["Workerservice_To_Webservice_Destination"] ?? "", ""),
        new GeneralMessagePacketHelper(configuration["Workerservice_To_Webservice_General"] ?? "", ""),
        new WeightScanMessagePacketHelper(configuration["Workerservice_To_Webservice_WeightScan"] ?? "", "")
    })
    {
        _serviceProvider = serviceProvider;

        var scope = serviceProvider.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<MessageDistributorService>();
        _configurationService = scope.ServiceProvider.GetRequiredService<ConfigurationService>();
        _client = scope.ServiceProvider.GetRequiredService<MqttClient>();
        _wmsClient = scope.ServiceProvider.GetRequiredService<WMS_Client>();
        _destinationService = scope.ServiceProvider.GetRequiredService<DestinationService>();

        AddHeaders(configuration);

        if (configuration["hub_url"] is not {} hubUrl)
            _logger.LogWarning("The url for the hub connection is null or empty.");
        else
        {
            _hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(hubUrl)
                .Build();

            _hubConnection
                .On<DestinationStatus>("ReceiveDestinationStatus", _destinationService.OnNewDestinationStatus);

            _hubConnection.StartAsync();
        }
        
        _logger.LogInformation("The message distributor service has been started successfully");
    }


    private void AddHeaders(IConfiguration configuration)
    {
        var key = configuration["key"] ?? string.Empty;

        if (string.IsNullOrEmpty(key))
            _logger.LogWarning("The api key is empty");
        else
            _wmsClient?.AddHeader("x-api-key", key);
    }

    public override void DistributeIncomingMessages(object? sender, MessagePacketEventArgs messageEvent)
    {
        if (messageEvent.Message is null) //TODO: logging
            return;

        var pckHelper = _packetHelpers.FirstOrDefault(_ => _.InTopic == messageEvent.Message.Topic);

        if (pckHelper != null)
        {
            pckHelper.SetPacketData(messageEvent.Message);

            if (messageEvent.Message.Topic == CommonData.Topics[TopicType.Workerservice_Webservice_Config])
                OnConfigurationMessage(pckHelper);
            
            else if (messageEvent.Message.Topic == CommonData.Topics[TopicType.Workerservice_Webservice])
                OnShipmentMessageAsync(pckHelper);

            else if (messageEvent.Message.Topic == CommonData.Topics[TopicType.Workerservice_Webservice_Destination])
                OnDestinationMessageAsync(pckHelper);

            else if (messageEvent.Message.Topic == CommonData.Topics[TopicType.Workerservice_Webservice_General])
                OnGeneralPacket(pckHelper);

            else if (messageEvent.Message.Topic == CommonData.Topics[TopicType.Workerservice_Webservice_WeightScan]) 
                OnWeightScanAsync(pckHelper);
        }
    }

    private async void OnShipmentMessageAsync(MessagePacketHelper pckHelper)
    {
        if (pckHelper is not ShipmentPacketHelper packetHelper) 
            return;


        if (packetHelper.ShipmentPacket.KeyCode == ActionKey.RequestedEntity)
        {
            using var scope = _serviceProvider.CreateScope();
            var shipmentService = scope.ServiceProvider.GetRequiredService<ShipmentService>();

            if (packetHelper.ShipmentPacket.RequestedShipments is {Count: > 0})
            {
                var shipments = await shipmentService.GetShipments(packetHelper.ShipmentPacket.RequestedShipments);
                SendUpdatedShipments(shipments.ToArray());
            }
            else
            {
                var shipments = await shipmentService.GetHistoricalShipments();
                SendNewShipments(shipments.ToArray());
            }
        }
        else if (packetHelper.ShipmentPacket.KeyCode == ActionKey.UpdatedEntity)
        {
            if (packetHelper.ShipmentPacket is null || packetHelper.ShipmentPacket.Shipments is null)
                _logger.LogWarning("The shipment packet or the shipments of the shipment packet is null.");
            else
                PatchShipments(packetHelper.ShipmentPacket.Shipments);
        }
    }

    private async void PatchShipments(List<Shipment> shipments)
    {
        using var scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ShipmentService>().UpdateShipments(shipments);

        var numberOfAttempts = 0;
        var maxNumberOfAttempts = 50;

        while (numberOfAttempts++ <= maxNumberOfAttempts)
        {
            try
            {
                await _wmsClient.PatchAsync($"{CommonData.Endpoints[API_Endpoint.PATCH_Schipment]}/" +
                                            $"{shipments.First().Id}", shipments.First());

                _logger.LogInformation($"Shipment ({shipments.First()}) has been patched");
                break;
            }
            catch (HttpRequestException) { await Task.Delay(1000); } 
        }

        _logger.LogInformation($"Number of attempts to patch shipment in WMS: {numberOfAttempts}. " +
                               $"Shipments: {(shipments != null ? string.Join(", ", shipments) : "null")}");
    }

    private async void OnConfigurationMessage(MessagePacketHelper pckHelper)
    {
        var packetHelper = pckHelper as ConfigurationPacketHelper;
            
        if (packetHelper.ConfigurationPacket.KeyCode == ActionKey.RequestedEntity)
        {
            var currentServiceConfig = await _configurationService.GetActiveConfiguration();
            SendNewServiceConfiguration(currentServiceConfig);
        }
    }

    public void SendNewServiceConfiguration(ServiceConfiguration config)
    {
        var pckHelper = new ConfigurationPacketHelper("", CommonData.Topics[TopicType.WebService_Workerservice_Config]);
        pckHelper.CreateNewConfigurationResponse(config);

        _client.SendData(pckHelper.GetPacketData());
    }

    public void SendNewShipments(params Shipment[] shipments)
    {
        var pckHelper = new ShipmentPacketHelper("", CommonData.Topics[TopicType.WebService_Workerservice]);
        pckHelper.CreateNewShipmentsRespose(shipments);

        _client.SendData(pckHelper.GetPacketData());
    }

    public void SendUpdatedShipments(params Shipment[] shipments)
    {
        var pckHelper = new ShipmentPacketHelper("", CommonData.Topics[TopicType.WebService_Workerservice]);
        pckHelper.CreateUpdatedShipmentsResponse(shipments);

        _client.SendData(pckHelper.GetPacketData());
    }

    private async void OnDestinationMessageAsync(MessagePacketHelper messagePacketHelper)
    {
        try
        {
            if (messagePacketHelper is DestinationPacketHelper { DestinationPacket: not null } pckHelper)
            {
                if (pckHelper.DestinationPacket.KeyCode == ActionKey.RequestedEntity)
                    SendUpdatedDestinations((await _destinationService.GetDestinationsAsync()).ToArray());
            }
            else
            {
                _logger.LogWarning("The packethelper or destination packet is null.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }

    public void SendUpdatedDestinations(params Destination[] destinations)
    {
        var pckHelper = new DestinationPacketHelper("",
            CommonData.Topics[TopicType.Webservice_Workerservice_Destination]);

        pckHelper.CreateDestinationResponse(destinations);

        _client.SendData(pckHelper.GetPacketData());
    }

    #region generalPacket

    private void OnGeneralPacket(MessagePacketHelper packetHelper)
    {
        if (packetHelper is GeneralMessagePacketHelper {GeneralPacket: not null} pckHelper)
        {
            if (pckHelper.GeneralPacket.KeyCode == ActionKey.NewEntity)
            {
                GeneralPacketReceived?.Invoke(this, new()
                {
                    GeneralPacket = pckHelper.GeneralPacket,
                });
            }
        }
        else
        {
            _logger.LogWarning("The packethelper or general packet is null.");
        }
    }

    #endregion

    #region Weightscan

    private async void OnWeightScanAsync(MessagePacketHelper packetHelper)
    {
        try
        {
            if (packetHelper is WeightScanMessagePacketHelper {WeightScanPacket: not null} pckHelper)
            {
                if (pckHelper.WeightScanPacket.KeyCode == ActionKey.NewEntity && pckHelper.WeightScanPacket.WeightScan is { } weightScan)
                {
                    using var scope = _serviceProvider.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<WeightScanService>().AddWeightScan(weightScan);

                    PostScan(weightScan);
                }
            }
            else
            {
                _logger.LogWarning("The packethelper or weightscan packet is null.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }

    private async void PostScan(Scan? scan)
    {
        var numberOfAttempts = 0;
        var maxNumberOfAttempts = 50;

        while (numberOfAttempts++ <= maxNumberOfAttempts)
        {
            try
            {
                await _wmsClient.PostAsync(CommonData.Endpoints[API_Endpoint.POST_ScaleScan], scan);
                _logger.LogInformation($"Weightscan ({scan}) has been posted");
                break;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(1000);
            }
        }

        _logger.LogInformation($"Number of attempts to post weightscan in WMS: {numberOfAttempts}");
    }

    #endregion

    /// <summary>
    /// Labels will be downloaded async in the defined local file system.
    /// </summary>
    /// <param name="shipmentId"></param>
    public async void GetLabelsAsync(int shipmentId)
    {
        if (!string.IsNullOrWhiteSpace(CommonData.Endpoints[API_Endpoint.GET_Labels]))
        {
            var times = 0;
            Stream? stream = null;

            while (stream is null && ++times <= 10)
            {
                stream = await _wmsClient.GetStream($"{CommonData.Endpoints[API_Endpoint.GET_Labels]}/{shipmentId}/download");

                if (stream != null)
                    FileManager.SetZplFile(stream, shipmentId);
                else
                    await Task.Delay(TimeSpan.FromSeconds(30));
            }

            _logger.LogInformation($"The label request runs {times} time/s");
        }
        else
        {
            _logger.LogWarning("The API endpoint of labels is empty.");
        }
    }
}