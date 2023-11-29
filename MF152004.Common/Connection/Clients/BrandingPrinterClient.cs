using MF152004.Common.Machines;
using MF152004.Models.EventArgs;
using Microsoft.Extensions.Logging;
using ReaPiSharp;

namespace MF152004.Common.Connection.Clients;

public class BrandingPrinterClient
{
    public List<BrandPrinter> BrandPrinters { get; } = new();
        
    private readonly ILogger<BrandingPrinterClient> _logger;
    private ReaPi.ResponseHandle _responseHandle;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    // These local references keep the delegates from being garbage collected
    private readonly ReaPi.connectionCallbackPtr _connectionCallback;
    private readonly ReaPi.eventCallbackPtr _eventCallback;
    private readonly ReaPi.responseCallbackPtr _responseCallback;

    public event EventHandler<FinishedPrintJobEventArgs>? EndOfPrint;
        
    public BrandingPrinterClient(ILogger<BrandingPrinterClient> logger)
    {
        _logger = logger;

        _connectionCallback = OnConnectionCallback;
        _eventCallback = OnEventCallback;
        _responseCallback = OnResponseCallback;

        ReaPi.RegisterConnectionCallback(_connectionCallback, 0);
    }

    private void OnConnectionCallback(ReaPi.ConnectionIdentifier connectionId, ReaPi.EConnState state, ReaPi.EErrorCode errorCode, nint context)
    {
        if (errorCode != ReaPi.EErrorCode.OK)
            _logger.LogWarning("Received an errorCode {errorCode} in connection callback", errorCode);
            
        if (connectionId > 0 && state == ReaPi.EConnState.CONNECT)
        {
            ReaPi.RegisterEventCallback(connectionId, _eventCallback, 0);
            ReaPi.RegisterResponseCallback(connectionId, _responseCallback, 0);

            ReaPi.GetNetworkConfig(connectionId);
        }
        else if (state == ReaPi.EConnState.DISCONNECT)
        {
            DisconnectBrandPrinter(connectionId);
        }
        else if (state == ReaPi.EConnState.CONNECTIONERROR)
        {
            DisconnectBrandPrinter(connectionId, true);
        }
    }

    private void DisconnectBrandPrinter(ReaPi.ConnectionIdentifier connectionId, bool reconnect = false)
    {
        ReaPi.RegisterEventCallback(connectionId, null, 0);
        ReaPi.RegisterResponseCallback(connectionId, null, 0);

        var brandPrinter = BrandPrinters
            .FirstOrDefault(b => b.ConnectionId == connectionId);

        if (brandPrinter != null)
        {
            brandPrinter.IsConnected = false;
            brandPrinter.ConnectionId = ReaPi.ConnectionIdentifier.UNKNOWN;

            if (reconnect)
                ReconnectPrinter(brandPrinter);
        }
    }

    private void SetBrandPrinter(ReaPi.ConnectionIdentifier connectionId, string ipAddress)
    {
        var brandPrinter = BrandPrinters
            .FirstOrDefault(b => b.Settings.IPAddress == ipAddress);

        if (brandPrinter != null)
        {
            brandPrinter.ConnectionId = connectionId;
            brandPrinter.IsConnected = true;
                
            ReaPi.SubscribeJobSet(connectionId, brandPrinter.JobId); //this is calling the eventCallback/jobset 
        }
    }

    private void OnResponseCallback(ReaPi.ResponseHandle response, ReaPi.ConnectionIdentifier connectionId, ReaPi.ECommandId commandId, ReaPi.EErrorCode errorCode, nint context)
    {
        _responseHandle = response;

        if (errorCode != ReaPi.EErrorCode.OK)
            _logger.LogWarning($"Error response received by {this}. Error: {errorCode}");

        if (commandId == ReaPi.ECommandId.CMD_GETNETWORKCONFIG)
        {
            var ipAddress = ReaPi.GetIPAddress(response, out var error);
                
            if (error == 0 && !string.IsNullOrEmpty(ipAddress)) 
                SetBrandPrinter(connectionId, ipAddress);
        }
    }

    private void OnEventCallback(ReaPi.ResponseHandle response, ReaPi.ConnectionIdentifier connection, ReaPi.EEventId eventId, nint context)
    {
        _responseHandle = response;

        var brandPrinter = BrandPrinters.FirstOrDefault(b => b.ConnectionId == connection);

        if (brandPrinter is null) 
        {
            _logger.LogWarning($"An event has been received with an unknown connection identifier {connection}. Event: {eventId}");
            return;
        }

        switch (eventId)
        {
            case ReaPi.EEventId.JOBSET:

                var filename = ReaPi.GetJobFilename(_responseHandle, out var error);

                _logger.LogWarning($"ErrorCode at filename is {error}");

                if (string.IsNullOrEmpty(filename) || filename != brandPrinter.Settings.Configuration.Job)
                {
                    ReaPi.SetJob(connection, brandPrinter.JobId, brandPrinter.Settings.Configuration.Job);
                }
                else //else job is already available, job events will be subscribed
                {
                    _logger.LogInformation($"A job {brandPrinter.Settings.Configuration.Job} has been set for {brandPrinter}");

                    ReaPi.SubscribeJobStarted(connection, brandPrinter.JobId); //in event jobset verschieben
                    ReaPi.SubscribeJobStopped(connection, brandPrinter.JobId);
                    ReaPi.SubscribeReadyForNextContent(connection, brandPrinter.JobId, brandPrinter.Settings.Configuration.Group);
                    ReaPi.SubscribePrintStart(connection, brandPrinter.JobId);
                    ReaPi.SubscribePrintEnd(connection, brandPrinter.JobId);
                    ReaPi.SubscribePrintAborted(connection, brandPrinter.JobId);
                    ReaPi.StartJob(connection, brandPrinter.JobId);
                }

                break;

            case ReaPi.EEventId.PRINTSTART:

                _logger.LogInformation($"{brandPrinter}: print started for shipment: {brandPrinter.CurrentJob.ShipmentId} with reference {brandPrinter.CurrentJob.ReferenceId}");
                break;

            case ReaPi.EEventId.PRINTEND:

                EndOfPrint?.Invoke(this, new()
                {
                    Job = brandPrinter.CurrentJob,
                    BasePositionBrandPrinter = brandPrinter.BasePosition
                });

                break;

            case ReaPi.EEventId.PRINTABORTED:
                
                _logger.LogInformation($"{brandPrinter}: Print has been aborted");
                break;

            case ReaPi.EEventId.JOBSTARTED:

                brandPrinter.JobIsStopped = false;
                _logger.LogInformation($"{brandPrinter}: Job has been started");

                break;

            case ReaPi.EEventId.JOBSTOPPED:

                brandPrinter.JobIsStopped = true;
                _logger.LogInformation($"{brandPrinter}: Job has been stopped. All print jobs will be removed.");
                brandPrinter.ClearJobs();

                break;

            case ReaPi.EEventId.READYFORNEXTCONTENT:
                brandPrinter.ReadyForNextContent = true;
                break;
        }
    }

    private async void ReconnectPrinter(BrandPrinter brandPrinter)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));

        ConnectBrandPrinter(brandPrinter);
    }

    public void ConnectBrandPrinter(BrandPrinter? brandPrinter) //settings are already checked in the printer object
    {
        if (brandPrinter is null) 
            return;

        if (BrandPrinters.All(b => b.BasePosition != brandPrinter.BasePosition))
            BrandPrinters.Add(brandPrinter);

        if (!brandPrinter.ErrorInSettings)
            ReaPi.Connect($"TCP://{brandPrinter.Settings.IPAddress}:{brandPrinter.Settings.Port}");
        else
            _logger.LogWarning($"BrandPrinter {brandPrinter} will not be connected. Wrong settings");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scannerBasePosition"></param>
    /// <param name="referenceId">The branding reference ID</param>
    /// <param name="shipmentId"></param>
    /// <returns>True if the print-command was successfully</returns>
    public bool Print(string? scannerBasePosition, string referenceId, int shipmentId)
    {
        if (scannerBasePosition is null)
            return false;

        var brandPrinter = BrandPrinters
            .FirstOrDefault(b => b.RelatedScanner != null && b.RelatedScanner.BasePosition == scannerBasePosition);

        if (BrandPrinterIsReady(brandPrinter))
        {
            brandPrinter?.Print(referenceId, shipmentId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scannerBasePosition"></param>
    /// <param name="shipmentId"></param>
    /// <returns>True if the print-command was successfully</returns>
    public bool TransparentPrint(string? scannerBasePosition, int shipmentId)
    {
        if (scannerBasePosition is null)
            return false;

        var brandPrinter = BrandPrinters
            .FirstOrDefault(b => b.RelatedScanner != null && b.RelatedScanner.BasePosition == scannerBasePosition);

        if (BrandPrinterIsReady(brandPrinter))
        {
            brandPrinter?.TransparentPrint(shipmentId);
            return true;
        }

        return false;
    }

    private bool BrandPrinterIsReady(BrandPrinter? brandPrinter)
    {
        if (brandPrinter is null)
        {
            _logger.LogError("No brand printer could be found");
            return false;
        }

        if (!brandPrinter.IsConnected)
        {
            _logger.LogWarning($"{brandPrinter} is not connected");
            return false;
        }

        if (brandPrinter.JobIsStopped)
        {
            _logger.LogWarning($"{brandPrinter}: print cannot be executed. The printer has the status: JOB-STOPPED");
            return false;
        }

        return true;
    }

    ~BrandingPrinterClient()
    {
        ReaPi.RegisterConnectionCallback(null, 0);

        foreach (var printer in BrandPrinters)
        {
            ReaPi.RegisterEventCallback(printer.ConnectionId, null, 0);
            ReaPi.RegisterResponseCallback(printer.ConnectionId, null, 0);
        }
    }
}