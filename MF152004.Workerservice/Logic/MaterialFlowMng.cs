﻿using BlueApps.MaterialFlow.Common.Logic;
using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Connection.PacketHelper;
using MF152004.Workerservice.Sectors;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Common;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Workerservice.Services;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Common.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Sectors.Gates;
using MF152004.Common.Machines;
using MF152004.Common.Connection.Clients;
using MF152004.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using MF152004.Models.Values.Types;
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
    private readonly BrandPrinterSettingsBack _brandPrinterSettingsBack;
    private readonly BrandPrinterSettingsFront _brandPrinterSettingsFront;
    private readonly ILogger<BrandPrinter> _brandPrinterLogger;
    private readonly BrandingPrinterClient _brandingPrinterClient;

    private CancellationToken _cancellationToken;

    //private readonly List<MessagePacketHelper> _packetHelpers; //class => MessageDistributor

    public MaterialFlowMng(
        ILogger<MaterialFlowMng> logger, 
        MqttClient client, 
        ContextService contextService, 
        IConfiguration configuration, 
        IServiceProvider serviceProvider, 
        IOptions<BrandPrinterSettingsBack> brandPrinterSettingsBack,
        IOptions<BrandPrinterSettingsFront> brandPrinterSettingsFront,
        BrandingPrinterClient brandingPrinterClient)
    {
        _logger = logger;
        _client = client;
        _contextService = contextService;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _brandPrinterSettingsBack = brandPrinterSettingsBack.Value;
        _brandPrinterSettingsFront = brandPrinterSettingsFront.Value;
        _brandingPrinterClient = brandingPrinterClient;
            
        using var scope = serviceProvider.CreateScope();
        _destinationService = scope.ServiceProvider.GetRequiredService<DestinationService>();
        var msgDistributorLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<MessageDistributor>();
        var sectorServicesLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SectorServices>();
        _brandPrinterLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BrandPrinter>();

        SetTopics();
        SetLabelPrinterMessage();
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

    private void SetLabelPrinterMessage()
    {
        var msg = _configuration["label_printer_no_match_procedure_msg"];

        if (msg != null)
            CommonData.LabelprinterNoMatchMsg = msg;
    }

    /// <summary>
    /// Starts the materialflow - connection to broker / sends request of information
    /// </summary>
    /// <param name="cancellationToken">To stop the delay when connection can't be established</param>
    public async void Run(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        await PrepareClient();

        SubscribeEvents();
        //TODO: tauschen ↨ ??
        Sectors = CreateSectors();
        _destinationService.SetSectors(Sectors.ToList());
        _sectorServices.RunService(Sectors.ToList());
        
        await PrepareContextService();

        _logger.LogInformation("Destinations will be requested");

#if !SAFE_DEBUG
        _msgDistributor.SendDestinationsRequest(); //request destination configuration after start
#endif
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
                _logger.LogWarning("Couldn't connect to broker. Next connection establishment in 60 secs.");

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
        await Task.Delay(500); //wait for destinationservice

        var destinations = _destinationService.GetSectorsDestinations();

        if (destinations != null)
        {
            _msgDistributor.SendDestinationStatusToHub(destinations.ToArray());
        }
    }

    private void UpdateShipmentsDestination(object? sender, UpdateDestinationsEventArgs e)
    {
        var shipmentIdsToUpdate = _contextService.GetRunningShipments().Select(shipment => shipment.Id).ToArray();
        _msgDistributor.SendShipmentsRequest(shipmentIdsToUpdate);
    }

    private async void OnClientDisconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning("Connection to broker has been lost.");
        await ConnectClient();
    }

    protected override List<Sector> CreateSectors() //TODO: Funktionalität testen und refactoring
    {
        var boxSealer = new BoxSealerSector(_client, "2.2", _contextService, _msgDistributor);
        var brandPrinterSector = new BrandPrinterSector(_client, "3.1", _contextService, _msgDistributor);
        var scale = new Sectors.ScaleSector(_client, "3.2", _contextService, _msgDistributor);
        var labelPrinterSector = new LabelPrinterSector(_client, "3.3", _contextService, _msgDistributor, _configuration["hub_url"]);
        var exportSector = new ExportGates(_client, "5-6", _contextService, _msgDistributor);
        var telescopeSectorA = new TelescopeGatesSectorA(_client, "6-7", _contextService, _msgDistributor);
        var telescopeSectorB = new TelescopeGatesSectorB(_client, "7-8", _contextService, _msgDistributor);

#if !SAFE_DEBUG
        _msgDistributor.WeightScanned += scale.Weight_Scanned;
        _msgDistributor.LabelPrinterRefRequest += labelPrinterSector.RepeatLastPrinterReferenceBroadcast;
#endif

        _brandingPrinterClient.ConnectBrandPrinter(GetFrontBrandPrinter());
        _brandingPrinterClient.ConnectBrandPrinter(GetBackBrandPrinter());

        brandPrinterSector.AddBrandingPrinterClient(_brandingPrinterClient);

#if SAFE_DEBUG
        _msgDistributor.BarcodeScanned += brandPrinterSector.Barcode_Scanned;
#endif

        labelPrinterSector.AddLabelPrinter(GetFrontLabelPrinter());
        labelPrinterSector.AddLabelPrinter(GetBackLabelPrinter());

        List<Sector> sectors = new() //TODO: Design anpassen und Subscribes unterbringen! Ggf. allgemeines IncommíngData definieren
        {
            boxSealer,
            brandPrinterSector,
            scale,
            exportSector, 
            telescopeSectorA, 
            telescopeSectorB, 
            labelPrinterSector
        };

        using var scope = _serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Sector>();

        sectors.ForEach(sector =>
        {
#if !SAFE_DEBUG
            _msgDistributor.BarcodeScanned += sector.Barcode_Scanned;
            _msgDistributor.UnsubscribedPacket += sector.UnsubscribedPacket;
            _msgDistributor.ErrorCodeTriggered += sector.ErrorTriggered;
#endif

            sector.AddLogger(logger);
        });

        return sectors;
    }

    private BrandPrinter GetFrontBrandPrinter() => new(_brandPrinterSettingsFront, _brandPrinterLogger)
    {
        Name = "Brandprinter front",
        BasePosition = BrandPrinterPosition.BP1.ToString(),
        RelatedScanner = new("M3.1.189", "S3.1.190")
    };

    private BrandPrinter GetBackBrandPrinter() => new(_brandPrinterSettingsBack, _brandPrinterLogger)
    {
        Name = "Brandprinter back",
        BasePosition = BrandPrinterPosition.BP2.ToString(),
        RelatedScanner = new("M3.1.211", "S3.1.401")
    };

    private LabelPrinter GetFrontLabelPrinter() => new()
    {
        Name = "Frontprinter",
        Id = "1",
        RelatedScanner = new("M3.2.195", "S3.2.196") {Name = "Frontscanner"},
        IP = "192.168.42.12",
        Port = 6101
    };

    private LabelPrinter GetBackLabelPrinter() => new()
    {
        Name = "Backprinter",
        Id = "2",
        RelatedScanner = new("M3.2.195", "S3.2.196") {Name = "Frontscanner"},
        IP = "192.168.42.13", //TODO: aus appsettings
        Port = 6101
    };

    private async Task PrepareContextService() //Fragt Daten beim Webservice an
    {
#if SAFE_DEBUG
        return;
#endif

        var timeLimitSeconds = 60;
        var timeLimit = DateTime.Now.AddSeconds(timeLimitSeconds);

        while (!_contextService.ContextHasRequiredEntities())
        {
            _msgDistributor.SendConfigurationRequest();

            await Task.Delay(200, _cancellationToken);

            _msgDistributor.SendShipmentsRequest();

            await Task.Delay(2000, _cancellationToken);

            if (DateTime.Now > timeLimit)
            {
                _logger.LogWarning("The workerservice still has not received any entities. The request will continue.");
                timeLimit = DateTime.Now.AddSeconds(timeLimitSeconds);
            }
        }

        _logger.LogInformation("Entities could be received");
    }
}