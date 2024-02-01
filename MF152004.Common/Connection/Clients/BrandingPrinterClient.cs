using MF152004.Common.Machines;
using MF152004.Models.EventArgs;
using Microsoft.Extensions.Logging;
using ReaPiSharp;

namespace MF152004.Common.Connection.Clients
{
    public class BrandingPrinterClient
    {
        public List<Brandprinter> Brandprinters { get; } = new();
        
        private readonly ILogger<BrandingPrinterClient> _logger;
        private ReaPi.ResponseHandle _responseHandle;

        public event EventHandler<FinishedPrintJobEventArgs>? EndOfPrint;

        private readonly ReaPi.connectionCallbackPtr _connectionCallback;
        private readonly ReaPi.eventCallbackPtr _eventCallback;
        private readonly ReaPi.responseCallbackPtr _responseCallback;

        public BrandingPrinterClient(ILogger<BrandingPrinterClient> logger)
        {
            _logger = logger;

            _connectionCallback = OnConnectionCallback;
            _eventCallback = OnEventCallback;
            _responseCallback = OnResponseCallback;

            ReaPi.RegisterConnectionCallback(_connectionCallback, 0);
        }

        private void OnConnectionCallback(ReaPi.ConnectionIdentifier connectionid, ReaPi.EConnState state, ReaPi.EErrorCode errorcode, nint context)
        {
            if (errorcode != ReaPi.EErrorCode.OK)
                _logger.LogWarning($"Received an errorcode {errorcode} in connection callback");
            
            if (connectionid > 0 && state == ReaPi.EConnState.CONNECT)
            {
                ReaPi.RegisterEventCallback(connectionid, _eventCallback, 0);
                ReaPi.RegisterResponseCallback(connectionid, _responseCallback, 0);

                ReaPi.GetNetworkConfig(connectionid);
            }
            else if (state == ReaPi.EConnState.DISCONNECT)
            {
                DisconnectBrandprinter(connectionid);
            }
            else if (state == ReaPi.EConnState.CONNECTIONERROR)
            {
                DisconnectBrandprinter(connectionid, true);
            }
        }

        private void DisconnectBrandprinter(ReaPi.ConnectionIdentifier connectionid, bool reconnect = false)
        {
            ReaPi.RegisterEventCallback(connectionid, null, 0);
            ReaPi.RegisterResponseCallback(connectionid, null, 0);

            var brandprinter = Brandprinters
                    .FirstOrDefault(b => b.ConnectionId == connectionid);

            if (brandprinter != null)
            {
                brandprinter.IsConnected = false;
                brandprinter.ConnectionId = ReaPi.ConnectionIdentifier.UNKNOWN;

                if (reconnect)
                    ReconnectPrinter(brandprinter);
            }
        }

        private void SetBrandprinter(ReaPi.ConnectionIdentifier connectionid, string ipAddress)
        {
            var brandprinter = Brandprinters
                        .FirstOrDefault(b => b.Settings.IPAddress == ipAddress);

            if (brandprinter != null)
            {
                brandprinter.ConnectionId = connectionid;
                brandprinter.IsConnected = true;
                

                ReaPi.SubscribeJobSet(connectionid, brandprinter.JobId); //this is calling the eventCallback/jobset 
            }
        }

        private void OnResponseCallback(ReaPi.ResponseHandle response, ReaPi.ConnectionIdentifier connection, ReaPi.ECommandId commandid, ReaPi.EErrorCode errorcode, nint context)
        {
            _responseHandle = response;

            if (errorcode != ReaPi.EErrorCode.OK)
                _logger.LogWarning($"Responsed an error on {this}. Error: {errorcode}");

            if (commandid == ReaPi.ECommandId.CMD_GETNETWORKCONFIG)
            {
                string ipAddress = ReaPi.GetIPAddress(response, out int error);
                
                if (error == 0 && !string.IsNullOrEmpty(ipAddress))
                {
                    SetBrandprinter(connection, ipAddress); 
                }
            }
        }

        private void OnEventCallback(ReaPi.ResponseHandle response, ReaPi.ConnectionIdentifier connection, ReaPi.EEventId eventid, nint context)
        {
            _responseHandle = response;

            var brandprinter = Brandprinters.FirstOrDefault(b => b.ConnectionId == connection);

            if (brandprinter is null) 
            {
                _logger.LogWarning($"An event has been received with an unknown connection identifier {connection}." +
                    $" Event: {eventid}");
                return;
            }

            _logger.LogInformation("Brandprinter event received:{0} - {1}", brandprinter.Name, eventid);

            switch (eventid)
            {
                case ReaPi.EEventId.JOBSET:

                    var filename = ReaPi.GetJobFilename(_responseHandle, out int error);

                    _logger.LogWarning($"Errorcode at filename is {error}");

                    if (string.IsNullOrEmpty(filename) || filename != brandprinter.Settings.Configuration.Job)
                    {
                        ReaPi.SetJob(connection, brandprinter.JobId, brandprinter.Settings.Configuration.Job);
                    }
                    else //else job is already available, jobevents will be subscribed
                    {
                        _logger.LogInformation($"A job {brandprinter.Settings.Configuration.Job} has been " +
                            $"set for {brandprinter}");

                        ReaPi.SubscribeJobStarted(connection, brandprinter.JobId); //in event jobset verschieben
                        ReaPi.SubscribeJobStopped(connection, brandprinter.JobId);
                        ReaPi.SubscribeReadyForNextContent(connection, brandprinter.JobId, brandprinter.Settings.Configuration.Group);
                        ReaPi.SubscribePrintStart(connection, brandprinter.JobId);
                        ReaPi.SubscribePrintEnd(connection, brandprinter.JobId);
                        ReaPi.SubscribePrintAborted(connection, brandprinter.JobId);
                        ReaPi.StartJob(connection, brandprinter.JobId);
                    }

                    break;

                case ReaPi.EEventId.PRINTSTART:

                    _logger.LogInformation($"{brandprinter}: print started for shipment: {brandprinter.CurrentJob.ShipmentId} " +
                        $"with reference {brandprinter.CurrentJob.ReferenceId}");

                    break;

                case ReaPi.EEventId.PRINTEND:

                    EndOfPrint?.Invoke(this, new()
                    {
                        Job = brandprinter.CurrentJob,
                        BasePositionBrandPrinter = brandprinter.BasePosition
                    });

                    break;

                case ReaPi.EEventId.PRINTABORTED:

                    _logger.LogInformation($"{brandprinter}: Print has been aborted");


                    break;

                case ReaPi.EEventId.JOBSTARTED:

                    brandprinter.JobIsStopped = false;
                    _logger.LogInformation($"{brandprinter}: Job has been started");

                    break;

                case ReaPi.EEventId.JOBSTOPPED:

                    brandprinter.JobIsStopped = true;
                    _logger.LogInformation($"{brandprinter}: Job has been stopped. All printjobs will be removed.");
                    brandprinter.ClearJobs();

                    break;

                case ReaPi.EEventId.READYFORNEXTCONTENT:
                    brandprinter.ReadyForNextContent = true;
                    break;
            }
        }

        private async void ReconnectPrinter(Brandprinter brandprinter)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));

            ConnectBrandprinter(brandprinter);
        }

        public void ConnectBrandprinter(Brandprinter brandprinter) //settings are already checked in the printerobject
        {
            if (brandprinter is null)
                return;

            if (!Brandprinters.Any(b => b.BasePosition == brandprinter.BasePosition))
                Brandprinters.Add(brandprinter);

            if (!brandprinter.ErrorInSettings)
            {
                ReaPi.Connect($"TCP://{brandprinter.Settings.IPAddress}:{brandprinter.Settings.Port}");
            }
            else
            {
                _logger.LogWarning($"Brandprinter {brandprinter} will not be connected. Wrong settings");
            }
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

            var brandprinter = Brandprinters
                .FirstOrDefault(b => b.RelatedScanner != null && b.RelatedScanner.BasePosition == scannerBasePosition);

            if (BrandPrinterIsReady(brandprinter))
            {
                brandprinter?.Print(referenceId, shipmentId);
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

            var brandprinter = Brandprinters
                .FirstOrDefault(b => b.RelatedScanner != null && b.RelatedScanner.BasePosition == scannerBasePosition);

            if (BrandPrinterIsReady(brandprinter))
            {
                brandprinter?.TransparentPrint(shipmentId);
                return true;
            }

            return false;
        }

        private bool BrandPrinterIsReady(Brandprinter? brandPrinter)
        {
            if (brandPrinter is null)
            {
                _logger.LogError($"No brandprinter could be found");
                return false;
            }

            if (!brandPrinter.IsConnected)
            {
                _logger.LogWarning($"{brandPrinter} is not connected");
                return false;
            }

            if (brandPrinter.JobIsStopped)
            {
                _logger.LogWarning($"{brandPrinter}: print cannot be executed. " +
                            $"The printer has the status: JOB-STOPPED");

                return false;
            }

            return true;
        }

        ~BrandingPrinterClient()
        {
            ReaPi.RegisterConnectionCallback(null, 0);

            if (Brandprinters != null)
            {
                foreach (var printer in Brandprinters)
                {
                    ReaPi.RegisterEventCallback(printer.ConnectionId, null, 0);
                    ReaPi.RegisterResponseCallback(printer.ConnectionId, null, 0);
                } 
            }
        }
    }
}
