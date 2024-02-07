using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Connection.Packets.Events;
using BlueApps.MaterialFlow.Common.Connection.PackteHelper;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Common.Connection.Packets.PacketHelpers;
using MF152004.Models.Configurations;
using MF152004.Models.EventArgs;
using MF152004.Models.Main;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Models.Values.Types;
using Microsoft.AspNetCore.SignalR.Client;
using MF152004.Models.Connection.Packets.HubPacket;

namespace MF152004.Workerservice.Connection.Packets;

public class MessageDistributor : BlueApps.MaterialFlow.Common.Connection.Packets.MessageDistributor
{
    public override event EventHandler<BarcodeScanEventArgs>? BarcodeScanned;
    public override event EventHandler<WeightScanEventArgs>? WeigtScanned;
    public override event EventHandler<UnsubscribedPacketEventArgs>? UnsubscribedPacket;
    public override event EventHandler<ErrorcodeEventArgs>? ErrorcodeTriggered;

    public event EventHandler<NewShipmentEventArgs>? NewShipmentsReached;
    public event EventHandler<UpdateShipmentEventArgs>? UpdateShipmentsReached;
    public event EventHandler<DeleteShipmentEventArgs>? DeleteShipmentsReached;
    public event EventHandler<UpdateConfigurationEventArgs>? UpdateConfigurationReached;
    public event EventHandler<UpdateDestinationsEventArgs>? UpdateDestinationsReached;
    public event EventHandler<DockedTelescopeEventArgs>? DockedTelescopeReached;
    public event EventHandler<LoadFactorEventArgs>? LoadFactorReached;
    public event EventHandler? LabelPrinterRefRequest;

    private readonly MqttClient _client;
    private readonly ILogger<MessageDistributor> _logger;
    private HubConnection? _hubConnection;

    public MessageDistributor(
        List<MessagePacketHelper> packetHelpers, 
        MqttClient client, 
        string? hubUrl,
        ILogger<MessageDistributor> logger) : base(packetHelpers)
    {
        _client = client;
        _logger = logger;

        InitHubConnection(hubUrl);

        _logger.LogInformation("The message-distributor has been started successfully on workerservice");
    }

    private async void InitHubConnection(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("The url for the hub connection is null or empty");
        }
        else
        {
            _hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(url)
                .Build();

            while (true)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError("MessageDistributor: Cannot connect to the hub. Next try in 5 s. {0}", exception.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
    }

    public override void DistributeIncomingMessages(object? sender, MessagePacketEventArgs messageEvent)
    {
        try
        {
            var packetHelper = _packetHelpers.FirstOrDefault(helper => helper.InTopic == messageEvent.Message?.Topic);

            if (packetHelper == null)
            {
                _logger.LogWarning("{0}: Unknown packet received: {1}", nameof(DistributeIncomingMessages), messageEvent.Message);
                return;
            }

            packetHelper.SetPacketData(messageEvent.Message!);

            if (packetHelper.InTopic == CommonData.Topics[TopicType.PLC_Workerservice])
                DistributeMessageFromPLC(packetHelper);

            else if (packetHelper.InTopic == CommonData.Topics[TopicType.WebService_Workerservice])
                DistributeMessageFromWebservice(packetHelper);
            
            else if (packetHelper.InTopic == CommonData.Topics[TopicType.WebService_Workerservice_Config])
                DistributeMessageFromWebserviceConfig(packetHelper);
            
            else if (packetHelper.InTopic == CommonData.Topics[TopicType.Webservice_Workerservice_Destination])
                DistributeMessageFromWebserviceDestination(packetHelper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{0}: Error receiving packet: {1}", nameof(DistributeIncomingMessages), messageEvent.Message);
        }
    }

    #region plc
    private void DistributeMessageFromPLC(MessagePacketHelper packetHelper)
    {
        var pckHelper = packetHelper as PLC152004_PacketHelper;

        switch (pckHelper.Command)
        {
            case PLC_Command.C001:

                OnBarcodeScanned(pckHelper);

                break;

            case PLC_Command.C003:

                OnWeightScanned(pckHelper);

                break;

            case PLC_Command.C004:
                //TODO: Offen - mit Abranson besprechen
                break;

            case PLC_Command.C005:

                OnUnsubscripedPacket(pckHelper);

                break;

            case PLC_Command.C006:

                OnDockedTelescope(pckHelper);

                break;

            case PLC_Command.C007:
                OnErrorcodeTriggered(pckHelper);
                break;

            case PLC_Command.C010:
                OnLoadFactor(pckHelper);
                break;

            case PLC_Command.C011:
                LabelPrinterRefRequest?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void OnBarcodeScanned(PLC152004_PacketHelper pckHelper)
    {
        //TODO: Logging 
        var scan = new BarcodeScanEventArgs
        {
            Position = pckHelper.Areas[4],                
            Barcodes = GetBarcodes(pckHelper)
        };

        _ = int.TryParse(pckHelper.Areas[2], out var packetTracing);
        scan.PacketTracing = packetTracing;

        BarcodeScanned?.Invoke(this, scan);
    }

    private void OnWeightScanned(PLC152004_PacketHelper pckHelper)
    {
        _ = int.TryParse(pckHelper.Areas[2], out var packetTracing);
        _ = byte.TryParse(pckHelper.Areas[3], out var validHeight);
        _ = double.TryParse(pckHelper.Areas[5], out var weight);

        var scan = new WeightScanEventArgs_152004
        {
            Weight = weight,
            PacketTracing = packetTracing,
            Barcodes = GetBarcodes(pckHelper),
            ValidHeight = validHeight == 0 ? true : false,
            Position = pckHelper.Areas[4]
        };

        WeigtScanned?.Invoke(this, scan);
    }

    private List<string> GetBarcodes(PLC152004_PacketHelper pckHelper)
    {
        var barcodes = new List<string>();

        for (var i = 6; i < pckHelper.PacketSettings.AreaLengths.Length; i++)
        {
            if (!string.IsNullOrEmpty(pckHelper.Areas[i]))
            {
                barcodes.Add(pckHelper.Areas[i]);
            }
        }

        return barcodes;
    }

    private void OnUnsubscripedPacket(PLC152004_PacketHelper pckHelper)
    {
        UnsubscribedPacketEventArgs packet = new();

        _ = int.TryParse(pckHelper.Areas[2], out var packetTracing);
        packet.PacketTracing = packetTracing;

        UnsubscribedPacket?.Invoke(this, packet);
    }

    private void OnDockedTelescope(PLC152004_PacketHelper pckHelper)
    {
        DockedTelescopeEventArgs docked = new()
        {
            AtTime = DateTime.Now,
            Gates = pckHelper.Areas[3].Split(',').ToList()
        };

        DockedTelescopeReached?.Invoke(this, docked);
    }

    private void OnErrorcodeTriggered(PLC152004_PacketHelper packetHelper)
    {
        ErrorcodeEventArgs error = new()
        {
            Errorcodes = SplitErrorcodes(packetHelper.Areas[6])
        };

        if (error.Errorcodes.Count > 0)
        {
            ErrorcodeTriggered?.Invoke(this, error);
        }
    }

    private ICollection<short> SplitErrorcodes(string data)
    {
        var errorcodes = new List<short>();

        if (string.IsNullOrEmpty(data))
            return errorcodes;

        var existsErrorcodes = (Errorcode[])Enum.GetValues(typeof(Errorcode));

        foreach (var splittedCode in data.Split(','))
        {
            if (short.TryParse(splittedCode, out var validCode))
            {
                if (existsErrorcodes.Any(c => (short)c == validCode))
                    errorcodes.Add(validCode);
            }
        }

        return errorcodes;
    }

    private void OnLoadFactor(PLC152004_PacketHelper packetHelper)
    {
        var status = packetHelper.Areas[4].ToCharArray();

        LoadFactorEventArgs load = new()
        {
            AtTime = DateTime.Now
        };

        for (var i = 0; i < status.Length; i++)
        {
            if (status[i] == '1')
            {
                load.LoadFactors.Add(new() //Design: zur Vermeidung von falschen Daten
                {
                    Gate = (i + 1).ToString(),
                    Factor = 100d
                });
            }
            else if (status[i] == '0')
            {
                load.LoadFactors.Add(new()
                {
                    Gate = (i + 1).ToString(),
                    Factor = 0d
                });
            }
        }

        if (load.LoadFactors.Count > 0)
            LoadFactorReached?.Invoke(this, load);
    }

    #endregion

    #region configuration

    private void DistributeMessageFromWebserviceConfig(MessagePacketHelper packetHelper)
    {
        var pckHelper = (ConfigurationPacketHelper)packetHelper;

        switch (pckHelper.ConfigurationPacket.KeyCode)
        {
            case ActionKey.NewEntity:

                OnNewConfiguration(pckHelper.ConfigurationPacket.Configuration);

                break;
        }
    }

    private void OnNewConfiguration(ServiceConfiguration configuration)
    {
        UpdateConfigurationReached?.Invoke(this, new UpdateConfigurationEventArgs
        {
            ServiceConfiguration = configuration
        });
    }


    public void SendConfigurationRequest()
    {
        var pckHelper = new ConfigurationPacketHelper("", CommonData.Topics[TopicType.Workerservice_Webservice_Config]);
        pckHelper.CreateNewConfigurationRequest();

        _client.SendData(pckHelper.GetPacketData());
    }

    #endregion

    #region shipments

    private void DistributeMessageFromWebservice(MessagePacketHelper packetHelper)
    {
        var pckHelper = (ShipmentPacketHelper)packetHelper;

        switch (pckHelper.ShipmentPacket.KeyCode)
        {
            case ActionKey.UpdatedEntity:

                OnUpdateShipments(pckHelper);

                break;

            case ActionKey.NewEntity:

                OnNewShipments(pckHelper);

                break;
        }
    }

    private void OnUpdateShipments(ShipmentPacketHelper pckHelper)
    {
        var shipments = new UpdateShipmentEventArgs
        {
            UpdatedShipments = pckHelper.ShipmentPacket.Shipments
        };

        UpdateShipmentsReached?.Invoke(this, shipments);
    }

    private void OnNewShipments(ShipmentPacketHelper pckHelper)
    {
        var shipments = new NewShipmentEventArgs
        {
            NewShipments = pckHelper.ShipmentPacket.Shipments
        };

        NewShipmentsReached?.Invoke(this, shipments);
    }

    public void SendShipmentsRequest(params int[] requestedShipments)
    {
        var pckHelper = new ShipmentPacketHelper("", CommonData.Topics[TopicType.Workerservice_Webservice]);
        pckHelper.CreateNewShipmentsRequest(requestedShipments);

        _client.SendData(pckHelper.GetPacketData());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="shipment"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task SendShipmentUpdate(Shipment? shipment)
    {
        if (shipment != null)
        {
            var pckHelper = new ShipmentPacketHelper("", CommonData.Topics[TopicType.Workerservice_Webservice]);
            pckHelper.CreateUpdatedShipmentsResponse(shipment);

            await _client.SendData(pckHelper.GetPacketData());
        }
        else
        {
            _logger.LogError($"Shipment is null [at {nameof(SendShipmentUpdate)}]");
            // throw new ArgumentNullException(nameof(shipment)); VK: ???
        }
    }

    #endregion

    #region destination

    private void DistributeMessageFromWebserviceDestination(MessagePacketHelper packetHelper)
    {
        if (packetHelper is DestinationPacketHelper { DestinationPacket: { KeyCode: ActionKey.UpdatedEntity, Destinations: not null } } destPackHelper)
        {
            UpdateDestinationsEventArgs e = new()
            {
                UpdatedDestinations = destPackHelper.DestinationPacket.Destinations
            };

            UpdateDestinationsReached?.Invoke(this, e);
        }
    }

    public async Task SendDestinationsRequest()
    {
        var pckHelper = new DestinationPacketHelper("", CommonData.Topics[TopicType.Workerservice_Webservice_Destination]);
        pckHelper.CreateDestinationRequest();

        await _client.SendData(pckHelper.GetPacketData());
    }

    public async void SendDestinationStatusToHub(params Destination?[]? destinations)
    {
        if (destinations is null || destinations.Length == 0 || _hubConnection is null || _hubConnection.State == HubConnectionState.Disconnected)
            return;

        var dests = destinations
            .Select(d => new Destination { Id = d.Id, Active = d.Active }).ToList();

        await _hubConnection.InvokeAsync("SendDestinationStatus", new DestinationStatus { Destinations = dests });
    }

    #endregion

    #region general

    public async Task SendNoRead(NoRead noRead)
    {
        var pckHelper = new GeneralMessagePacketHelper("", CommonData.Topics[TopicType.Workerservice_Webservice_General]);
        pckHelper.ClearGeneralPacketContext();
        pckHelper.CreateNoReadContext(noRead);

        await _client.SendData(pckHelper.GetPacketData());
    }

    #endregion

    #region WeightScan

    public void SendWeightScan(Scan scan)
    {
        var pckHelper = new WeightScanMessagePacketHelper("",
            CommonData.Topics[TopicType.Workerservice_Webservice_WeightScan]);            
        pckHelper.CreateNewWeightScanResponse(scan);

        _client.SendData(pckHelper.GetPacketData());
    }

    #endregion
}