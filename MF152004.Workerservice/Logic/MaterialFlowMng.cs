using BlueApps.MaterialFlow.Common.Logic;
using BlueApps.MaterialFlow.Common.Connection.Client;
using MF152004.Workerservice.Sectors;
using BlueApps.MaterialFlow.Common.Connection.PackteHelper;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Common;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Workerservice.Services;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Common.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Sectors.Gates;
using MF152004.Common.Machines;
using MF152004.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using MF152004.Models.Settings.BrandPrinter;
using Microsoft.Extensions.Options;

namespace MF152004.Workerservice.Logic;

public class MaterialFlowMng : MaterialFlowManager
{
    private readonly ILogger<MaterialFlowMng> _logger;
    private readonly MqttClient _client;
    private readonly IConfiguration _configuration;
    private readonly MessageDistributor _msgDistributor;
    private readonly ContextService _contextService;
    private readonly DestinationService _destinationService;
    private readonly SectorServices _sectorServices;
    private readonly IServiceProvider _serviceProvider;

    private CancellationToken _cancellationToken;

    //private readonly List<MessagePacketHelper> _packetHelpers; //class => MessageDistributor

    public MaterialFlowMng(
        IServiceProvider serviceProvider,
        ILogger<MaterialFlowMng> logger, 
        MqttClient client, 
        ContextService contextService, 
        IConfiguration configuration)
    {
        _logger = logger;
        _client = client;
        _contextService = contextService;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
            
        using var scope = serviceProvider.CreateScope();
        _destinationService = scope.ServiceProvider.GetRequiredService<DestinationService>();
        var msgDistributorLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<MessageDistributor>();
        var sectorServicesLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SectorServices>();

        SetTopics();
        SetLabelprinterMessage();
        _sectorServices = new(sectorServicesLogger);

        _msgDistributor = new MessageDistributor(new List<MessagePacketHelper>
        {
            new PLC152004_PacketHelper(),
            new ShipmentPacketHelper(CommonData.Topics[TopicType.WebService_Workerservice], CommonData.Topics[TopicType.Workerservice_Webservice]),
            new ConfigurationPacketHelper(CommonData.Topics[TopicType.WebService_Workerservice_Config], string.Empty),
            new DestinationPacketHelper(CommonData.Topics[TopicType.Webservice_Workerservice_Destination], string.Empty),
            new GeneralMessagePacketHelper(CommonData.Topics[TopicType.Webservice_Workerservice_General], string.Empty)
        }, _client, configuration["hub_url"], msgDistributorLogger);
    }

    private void SetTopics()
    {
        CommonData.Topics = new Dictionary<TopicType, string>
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

    private void SetLabelprinterMessage()
    {
        var msg = _configuration["label_printer_no_match_procedure_msg"];

        if (msg != null)
            CommonData.LabelprinterNoMatchMsg = msg;
    }

    /// <summary>
    /// Starts the materialflow - connection to broker / sends request of information
    /// </summary>
    /// <param name="cancellationToken">To stop the delay when connection can't be established</param>
    public async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            _cancellationToken = cancellationToken;
            await PrepareClient();

            SubscribeEvents();
            Sectors = CreateSectors();
            _destinationService.SetSectors(Sectors.ToList());
            _sectorServices.RunService(Sectors.ToList());

            await PrepareContextService();

            _logger.LogInformation("Sending Destinations request...");
            await _msgDistributor.SendDestinationsRequest(); //request destination configuration after start
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in MaterialFlow.Run");
        }
    }

    private async Task PrepareClient()
    {
        SetClientsTopics();

        await ConnectClient();
    }

    private async Task ConnectClient()
    {
        while (!_client.IsConnected)
        {
            await _client.Connect(_cancellationToken);

            if (!_client.IsConnected)
            {
                _logger.LogWarning("Couldn't connect to MQTT broker. Trying again in 60 s");
                await Task.Delay(TimeSpan.FromSeconds(60), _cancellationToken);
            }
        }
    }

    private void SetClientsTopics()
    {
        _client.AddTopics(CommonData.Topics[TopicType.WebService_Workerservice_Config],
            CommonData.Topics[TopicType.WebService_Workerservice],
            CommonData.Topics[TopicType.PLC_Workerservice],
            CommonData.Topics[TopicType.Webservice_Workerservice_Destination],
            CommonData.Topics[TopicType.Webservice_Workerservice_General],
            CommonData.Topics[TopicType.Webservice_Workerservice_WeightScan]);
    }

    private void SubscribeEvents()
    {
        _client.OnReceivingMessage += _msgDistributor.DistributeIncomingMessages;
        _client.ClientDisconnected += OnClientDisconnected;
        //TODO: als singleton? DepInj? 
        /*
         * In der Klasse müssen dann alle events hinterlegt werden. Diese werden dann von allen Teilnehmenden objekten
         * aboniert z.B. Sector.
         */
        _msgDistributor.UpdateShipmentsReached += _contextService.UpdateShipments;
        _msgDistributor.NewShipmentsReached += _contextService.NewShipments;
        _msgDistributor.UpdateConfigurationReached += _contextService.UpdateConfiguration;
        _msgDistributor.UpdateDestinationsReached += _destinationService.OnDestinationsUpdate;
        _msgDistributor.UpdateDestinationsReached += UpdateShipmentsDestination;
        _msgDistributor.DockedTelescopeReached += _destinationService.OnDockedTelescope;
        _msgDistributor.DockedTelescopeReached += UpdateDestinationOnHub;
        _msgDistributor.LoadFactorReached += _destinationService.OnLoadFactor;
    }

    private async void UpdateDestinationOnHub(object? sender, DockedTelescopeEventArgs e)
    {
        try
        {
            await Task.Delay(500, _cancellationToken); //wait for destinationservice

            var destinations = _destinationService.GetSectorsDestinations();

            if (destinations != null)
            {
                await _msgDistributor.SendDestinationStatusToHub(destinations.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateDestinationOnHub failed");
        }
    }

    private void UpdateShipmentsDestination(object? sender, UpdateDestinationsEventArgs e)
    {
        var shipmentIdsToUpdate = _contextService.GetRunningShipments().Select(_ => _.Id).ToList();
        _msgDistributor.SendShipmentsRequest(shipmentIdsToUpdate.ToArray());
    }

    private async void OnClientDisconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning("Connection to broker has been lost");
        await ConnectClient();
    }

    protected override List<Sector> CreateSectors() //TODO: Funktionalität testen und refactoring
    {
        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Sector>();

        var boxSealer = new BoxSealerSector(_client, logger, "2.2", _contextService, _msgDistributor);
        var brandPrinterSector = new BrandPrinterSector(_client, logger, "3.1", _contextService, _msgDistributor, _configuration);
        var scale = new Sectors.ScaleSector(_client, logger, "3.2", _contextService, _msgDistributor);
        var labelPrinterSector = new LabelPrinterSector(_client, logger, "3.3", _contextService, _msgDistributor, _configuration["hub_url"]);
        var exportSector = new ExportGates(_client, logger, "5-6", _contextService, _msgDistributor);
        var telescopeSectorA = new TelescopeGatesSectorA(_client, logger, "6-7", _contextService, _msgDistributor);
        var telescopeSectorB = new TelescopeGatesSectorB(_client, logger, "7-8", _contextService, _msgDistributor);

        _msgDistributor.WeigtScanned += scale.Weight_Scanned;
        _msgDistributor.LabelPrinterRefRequest += labelPrinterSector.RepeatLastPrinterReferenceBroadcast;

        labelPrinterSector.AddLabelPrinters(GetFrontLabelPrinter());
        labelPrinterSector.AddLabelPrinters(GetBackLabelPrinter());

        List<Sector> sectors = new() //TODO: Design anpassen und Subscribes unterbringen! Ggf. allgemeines IncommíngData definieren
        {
            boxSealer, brandPrinterSector, scale, exportSector, telescopeSectorA, telescopeSectorB, labelPrinterSector
        };

        sectors.ForEach(sector =>
        {
            _msgDistributor.BarcodeScanned += sector.Barcode_Scanned;
            _msgDistributor.UnsubscribedPacket += sector.UnsubscribedPacket;
            _msgDistributor.ErrorcodeTriggered += sector.ErrorTriggered;
        });

        return sectors;
    }

    private LabelPrinter GetFrontLabelPrinter()
    {
        var printer = new LabelPrinter
        {
            Name = "Frontprinter",
            Id = "1",
            RelatedScanner = new("M3.2.195", "S3.2.196") { Name = "Frontscanner" },
            IP = "192.168.42.12",
            Port = 6101
        };

        return printer;
    }

    private LabelPrinter GetBackLabelPrinter()
    {
        var printer = new LabelPrinter
        {
            Name = "Backprinter",
            Id = "2",
            RelatedScanner = new("M3.2.195", "S3.2.196") { Name = "Frontscanner" },
            IP = "192.168.42.13", //TODO: aus appsettings
            Port = 6101
        };

        return printer;
    }

    private async Task PrepareContextService() //Fragt Daten beim Webservice an
    {
        var timeLimit = 60000;
        var time = 0;

        while (!_contextService.ContextHasRequiredEntities())
        {
            _msgDistributor.SendConfigurationRequest();

            await Task.Delay(200);

            _msgDistributor.SendShipmentsRequest();

            await Task.Delay(2000);

            time += 2000;

            if (time >= timeLimit)
            {
                _logger.LogWarning("The workerservice still has not received any entities. The request will continue.");
                time = 0;
            }
        }

        _logger.LogInformation("Entities could be received");
    }
}