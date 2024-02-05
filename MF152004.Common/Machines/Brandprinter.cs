using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Models.Machines;
using MF152004.Models.Settings.BrandPrinter;
using MF152004.Models.Values;
using Microsoft.Extensions.Logging;
using ReaPiSharp;
using System.Collections.Concurrent;

namespace MF152004.Common.Machines;

public class Brandprinter : IMachine
{
    public IBrandPrinterSettings Settings { get; private set; }
    public ReaPi.ConnectionIdentifier ConnectionId { get; set; } = ReaPi.ConnectionIdentifier.UNKNOWN;
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string BasePosition { get; set; }
    public string SubPosition { get; set; }

    public List<int> TracedPackets { get; set; } = new();
    public Scanner? RelatedScanner { get; set; }
    public int JobId { get; private set; } = 1;
    public bool IsConnected
    {
        get
        {
            lock (_lockConnect)
            {
                return _isConnected;
            }
        }

        set
        {
            lock (_lockConnect)
            {
                _isConnected = value;
            }
        }
    }
    public bool ReadyForNextContent
    {
        get
        {
            lock (_lockReadyForNextContent)
            {
                return _readyForNextContent;
            }
        }

        set
        {
            lock (_lockReadyForNextContent)
            {
                _readyForNextContent = value;
            }
        }
    }
    public bool QueueIsProcessed
    {
        get
        {
            lock (_lockPrint)
            {
                return _queueIsProcessed;
            }
        }

        private set
        {
            lock (_lockPrint)
            {
                _queueIsProcessed = value;
            }
        }
    }
    public bool JobIsStopped
    {
        get
        {
            lock (_lockJobstatus)
            {
                return _jobIsStopped;
            }
        }

        set
        {
            lock (_lockJobstatus)
            {
                _jobIsStopped = value;
            }
        }
    }
    public bool ErrorInSettings { get; private set; }
    public bool NoJob { get; private set; }
    public PrintJob CurrentJob { get; private set; }


    private static object _lockReadyForNextContent = new object();
    private static object _lockPrint = new object();
    private static object _lockConnect = new object();
    private static object _lockJobstatus = new object();

    private readonly ConcurrentQueue<PrintJob> _printJobs = new ConcurrentQueue<PrintJob>();
    private readonly ILogger<Brandprinter> _logger;        

    private bool _readyForNextContent;
    private bool _queueIsProcessed;
    private bool _isConnected;
    private bool _jobIsStopped = true;
    private PrintJob _jobBefore = new PrintJob { ReferenceId = "0", ShipmentId = 0 };


    public Brandprinter(IBrandPrinterSettings settings, ILogger<Brandprinter> logger)
    {
        Settings = settings;
        _logger = logger;

        ValidateSettings();
        CurrentJob = new()
        {
            ReferenceId = Settings.Configuration.NoPrintValue,
            ShipmentId = 0
        };
    }

    private void ValidateSettings()
    {
        if (Settings is null
            || string.IsNullOrEmpty(Settings.IPAddress)
            || Settings.Port < 1
            || string.IsNullOrEmpty(Settings.Configuration.Job))
        {
            _logger.LogError($"Invalid settings for brandprinter {this}");
            ErrorInSettings = true;
        }
    }

    /// <summary>
    /// Print will be executed only if the machine is connected, job ist not stopped and a job is setted
    /// </summary>
    /// <param name="refId"></param>
    /// <param name="shipmentId"></param>
    public async void Print(string? refId, int shipmentId)
    {
        if (!IsConnected || JobIsStopped || NoJob) return;

        if (string.IsNullOrEmpty(refId))
        {
            _logger.LogWarning($"Reference ID is empty. Print will not be executed for shipment {shipmentId}.");
            return;
        }

        var printJob = new PrintJob { ShipmentId = shipmentId, ReferenceId = refId };
        _printJobs.Enqueue(printJob);

        if (!QueueIsProcessed)
        {
            QueueIsProcessed = true;

            while (_printJobs.Any())
            {
                await ContextCannotBeSet();

                if (_printJobs.TryDequeue(out var job))
                {
                    CurrentJob = job;

                    // if (job.ReferenceId != _jobBefore.ReferenceId)
                    {
                        var labelContent = ReaPi.CreateLabelContent();

                        ReaPi.PrepareLabelContent(labelContent, JobId, Settings.Configuration.Group,
                            Settings.Configuration.Object,
                            Settings.Configuration.Content,
                            job.ReferenceId);
                        ReaPi.SetLabelContent(ConnectionId, labelContent);
                    }

                    _jobBefore = job;
                    ReadyForNextContent = false;
                }

                QueueIsProcessed = false;
            }
        }
    }

    public void TransparentPrint(int shipmentId)
    {
        _logger.LogInformation("Transparent print for next package with ID {0}", shipmentId);
        Print(Settings.Configuration.NoPrintValue, shipmentId);
    }

    public void ClearJobs() => _printJobs.Clear();

    private async Task ContextCannotBeSet()
    {
        while (!ReadyForNextContent)
            await Task.Delay(80);
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Name))
            return Name;
        else
            return "Undefined brandprinter";
    }
}