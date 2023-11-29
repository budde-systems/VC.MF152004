using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Models.EventArgs;
using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Common.Data;
using MF152004.Common.Machines;
using MF152004.Models.Connection.Packets.HubPacket;
using MF152004.Models.Values.Types;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets;
using MF152004.Workerservice.Connection.Packets.PacketHelpers;
using MF152004.Workerservice.Services;
using Microsoft.AspNetCore.SignalR.Client;

namespace MF152004.Workerservice.Sectors;

//TODO: Die Möglichkeit zu aktivieren und deaktivieren
public class LabelPrinterSector : Sector
{
    private const string NAME = "Label Printer";
    private const string NO_PRINTER = "0";

    private readonly ContextService _contextService;
    private readonly MessageDistributor _messageDistributor;
    private readonly PLC152004_PacketHelper _packetHelper = new();

    private List<LabelPrinter>? _labelPrinters;
    private int _invalidsInTheRow;
    private string _lastPrinterRef;

    private HubConnection? _hubConnection;
    private Status _status = Status.Labelprinter_Ok;

    public LabelPrinterSector(
        IClient client, 
        string baseposition, 
        ContextService contextService, 
        MessageDistributor messageDistributor,
        string? hubUrl) : base(client, NAME, baseposition)
    {
        _contextService = contextService;
        _messageDistributor = messageDistributor;
        AddRelatedErrorcodes();
        BarcodeScanners = new();
        BarcodeScanners.AddRange(CreateScanners());

        IsActive = true; //remove it
        InitHubConnection(hubUrl);
    }

    private async void InitHubConnection(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            _logger.LogWarning("The url for the hub connection is null or empty.");
        else
        {
            _hubConnection = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(url)
                .Build();

            _hubConnection.On<SystemStatus>("ReceiveStatus", OnMatchErrorConfirmation);

            while (true)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError($"No hub-connection could established. Next try in 5secs. ERROR:\n{exception}");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
    }

    public override void AddRelatedErrorcodes()
    {
        var errors = new List<Errorcode>
        {
            Errorcode.EmergencyHold_Labelprinter1,
            Errorcode.EmergencyHold_Labelprinter2,
            Errorcode.NoLabel1,
            Errorcode.NoLabel2, //TODO: weitere ergänzen
        };

        RelatedErrorcodes.AddRange(errors.Cast<short>());
    }

    public void AddLabelPrinters(LabelPrinter labelPrinter)
    {
        if (labelPrinter != null)
        {
            _labelPrinters ??= new();
            _labelPrinters.Add(labelPrinter);

            if (labelPrinter.RelatedScanner != null)
            {
                BarcodeScanners ??= new();

                if (!BarcodeScanners.Exists(_ => _.BasePosition == labelPrinter.RelatedScanner.BasePosition))
                    BarcodeScanners.Add(labelPrinter.RelatedScanner);
            }
            else
            {
                //throw..
            }
        }
    }

    private void OnMatchErrorConfirmation(SystemStatus status)
    {
        if (status.CurrentStatus == Status.Labelprinter_Confirmationstatus && _status == Status.Labelprinter_Matching_Error
                                                                           && status.Release)
        {
            _logger.LogInformation($"The match error status of shipment ID {status.TransportReference} on labelprinter " +
                                   "has been confirmed. The system will continue");
            _status = Status.Labelprinter_Ok;
            ContinueSystem("M3.2.198");
        }
    }

    public override void Barcode_Scanned(object? sender, BarcodeScanEventArgs scan)
    {
        if (BarcodeScanners.Exists(_ => _.BasePosition == scan.Position || _.SubPosition == scan.Position))
        {
            if (!IsActive)
            {
                _logger.LogInformation("Labelprinters are deactivated");
                return;
            }

            if (_labelPrinters is null || _labelPrinters.Count == 0)
            {
                _logger.LogWarning($"No labelprinters in sector {this}");
                return;
            }

            var scanPosition = BarcodeScanners.First(_ => _.BasePosition == scan.Position);
            var shipmentId = ValidateBarcodesAndGetShipmentId(scan.Barcodes?.ToArray());

            if (scanPosition.Name == "Frontscanner")
            {
                if (shipmentId > 0)
                {
                    if (_contextService.LabelIsPrinted(shipmentId))
                    {
                        NoPrint(shipmentId, scan);
                    }
                    else
                    {
                        PrintLabel(shipmentId, scan);
                    }
                }
            }
            else //inspectionscanner
            {
                if (shipmentId > 0)
                {
                    Inspection(shipmentId, scan);
                }
                else
                {
                    _packetHelper.Create_StopAndGo(scan.Position ?? "unknown", true);
                    _client.SendData(_packetHelper.GetPacketData()); //the gate is checking the LabelPrintedAt property
                }
            }

            if (shipmentId < 1)
                _logger.LogWarning($"Shipment ID is null at sector {this} at scanner {scan.Position}. Received barcodes: " +
                                   $"{(scan.Barcodes != null ? string.Join(", ", scan.Barcodes) : "null")}");

            OnFaultyBarcodes(scan);
        }
    }

    public void RepeatLastPrinterReferenceBroadcast(object? sender, EventArgs e)
    {
        _logger.LogInformation($"Printerreference is requested. The last printer reference is {_lastPrinterRef}");
            
        if (!string.IsNullOrEmpty(_lastPrinterRef))
        {
            _packetHelper.Create_LabelPrinter(_lastPrinterRef);
            _client.SendData(_packetHelper.GetPacketData()); 
        }
        else
        {
            _logger.LogWarning("The last labelprinter reference is empty");
        }
    }

    private void PrintLabel(int shipmentId, BarcodeScanEventArgs scan)
    {
        if (FileManager.ZplExists(shipmentId))
        {
            var file = FileManager.GetZplFile(shipmentId);
            var labelPrinterRef = _contextService.GetLabelPrinterReference(scan.Barcodes?.ToArray()) ?? string.Empty;
            var labelPrinter = _labelPrinters?.FirstOrDefault(_ => _.Id == labelPrinterRef);

            if (labelPrinter != null)
            {
                _lastPrinterRef = labelPrinterRef;
                labelPrinter.Print(file);
                _packetHelper.Create_LabelPrinter(labelPrinterRef);
                _client.SendData(_packetHelper.GetPacketData());
            }
            else
            {
                _logger.LogWarning($"No labelprinter could be found with labelprinter reference {labelPrinterRef} in sector {this}");
            }
        }
        else
        {
            _logger.LogWarning($"Zpl file could not be found with the ID {shipmentId} in sector {this}");
        }
    }

    private void NoPrint(int shipmentId, BarcodeScanEventArgs scan)
    {
        _lastPrinterRef = NO_PRINTER;
        _packetHelper.Create_LabelPrinter(NO_PRINTER);
        _client.SendData(_packetHelper.GetPacketData());

        _logger.LogInformation($"Label is already printed for shipment ID {shipmentId}");
    }

    private void Inspection(int shipmentId, BarcodeScanEventArgs scan)
    {
        if (_contextService.RelationShipIsValid(shipmentId, scan.Barcodes?.ToArray()))
        {
            _contextService.LabelPrintedAt(shipmentId, false);
            ContinueSystem("M3.2.198");
            _logger.LogInformation($"Valid relationship between shipment ID {shipmentId} and trackingcode. " +
                                   $"Received barcodes: {(scan.Barcodes != null ? string.Join(", ", scan.Barcodes) : "null")}");
        }
        else
        {
            var msg = $"The barcodes on package ID {shipmentId} could not be validated. " +
                      $"Received barcodes: {(scan.Barcodes != null ? string.Join(", ", scan.Barcodes) : "null")}.";

            if (_invalidsInTheRow++ > 1)
            {
                _invalidsInTheRow = 0;
                msg += " The package will stop";
                _status = Status.Labelprinter_Matching_Error;
                StopSystem(scan.Position);
                SendMessageToHub(_contextService.GetShipment(shipmentId)?.TransportationReference, scan.Barcodes);
            }
            else
            {
                ContinueSystem(scan.Position);
                msg += " The package will continue";
            }

            var errorcode = "1007";
            _contextService.SetMessage(errorcode + msg, shipmentId);
            _contextService.SetTarget(shipmentId, CommonData.FaultIsland);

            _contextService.LabelPrintedAt(shipmentId, true);
            _logger.LogWarning(msg);
        }

        _messageDistributor.SendShipmentUpdate(_contextService.GetShipment(shipmentId));
    }

    private async void SendMessageToHub(string? transportationRef, List<string>? barcodes)
    {
        if (_hubConnection != null)
        {
            while(_status == Status.Labelprinter_Matching_Error)
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("SendStatus", new SystemStatus
                    {
                        CurrentStatus = Status.Labelprinter_Matching_Error,
                        Message = CommonData.LabelprinterNoMatchMsg,
                        TransportReference = transportationRef ?? "Unbekannt",
                        ReadedCodes = barcodes != null ? string.Join(", ", barcodes) : "-"
                    });
                }
                else
                    _logger.LogWarning("The state of hubconnection is disconnected");

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            _logger.LogInformation("Sending to hub has been stopped");
        }            
    }

    private void ContinueSystem(string? scanPosition)
    {
        _packetHelper.Create_StopAndGo(scanPosition ?? "unknown", true);
        _client.SendData(_packetHelper.GetPacketData());
    }

    private void StopSystem(string? scanPosition)
    {
        _packetHelper.Create_StopAndGo(scanPosition ?? "unknown", false);
        _client.SendData(_packetHelper.GetPacketData());
    }

    private int ValidateBarcodesAndGetShipmentId(params string[]? barcodes) =>
        barcodes is null || barcodes.Any(_ => _ == CommonData.NoRead) ? -1 : _contextService.GetShipmentId(barcodes);

    private void OnFaultyBarcodes(BarcodeScanEventArgs scan)
    {
        if (scan.Barcodes is null)
        {
            _logger.LogWarning($"Barcodes is null in sector {this}");
        }
        else if (scan.Barcodes.Contains(CommonData.NoRead))
        {
            NoRead noRead = new()
            {
                AtTime = scan.AtTime,
                Position = scan.Position ?? string.Empty,
            };

            _messageDistributor.SendNoRead(noRead);

            _logger.LogWarning($"No Read was detected in barcodes (BCs: {string.Join(";", scan.Barcodes)})");
        }
    }

    public override ICollection<IDiverter> CreateDiverters() => default!; //not required

    public override Scanner CreateScanner() => default!; //not required

    public List<Scanner> CreateScanners()
    {
        var scanner2 = new Scanner("M3.2.198", "S3.1.199") { Name = "Inspectionscanner" };

        return new() { scanner2 };
    }

    public override void UnsubscripedPacket(object? sender, UnsubscribedPacketEventArgs unsubscribedPacket)
    {

    }

    protected override void ErrorHandling(short errorcode)
    {

    }
}