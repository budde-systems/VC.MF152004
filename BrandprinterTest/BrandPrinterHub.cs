using Microsoft.Extensions.Logging;
using ReaPiSharp;
using System.Collections.Concurrent;

namespace BrandprinterTest;

public class BrandPrinterConfig
{
    public string ConnectionString { get; set; }

    public string? Job { get; set; }
    public string? Group { get; set; }
    public string? Object { get; set; }
    public string? Content { get; set; }
    public string? Value { get; set; }
    public string? NoPrintValue { get; set; }
}


public class BrandPrinter
{
    public BrandPrinterConfig Settings { get; } = new BrandPrinterConfig();
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string BasePosition { get; set; }
    public string SubPosition { get; set; }
}

public class ReaPiException(string? message) : Exception(message);

public class BrandPrinterHub(ILogger<BrandPrinterHub> logger)
{
    private readonly ILogger<BrandPrinterHub> _logger = logger;

    private readonly object _connectionLock = new();
    private readonly Dictionary<string, Task<ReaPi.ConnectionIdentifier>> _connectionTasks = new();
    private readonly Dictionary<string, ReaPi.ConnectionIdentifier> _connections = new();
    private int _jobId;

    public async Task Print(BrandPrinter printer)
    {
        var connection = await ConnectAsync(printer);
        var jobId = Interlocked.Increment(ref _jobId);
        var response = ReaPi.SetJob(connection, jobId, "");
        response = ReaPi.StartJob(connection, jobId);

        
        
        var labelContent = ReaPi.CreateLabelContent();

        var error = ReaPi.PrepareLabelContent(labelContent, jobId, printer.Settings.Group,
            printer.Settings.Object,
            printer.Settings.Content,
            jobId.ToString());

        response = ReaPi.SetLabelContent(connection, labelContent);
    }


    private Task<ReaPi.ConnectionIdentifier> ConnectAsync(BrandPrinter printer)
    {
        lock (_connectionLock)
        {
            if (_connections.TryGetValue(printer.Id, out var connectionId)) 
                return Task.FromResult(connectionId);

            if (!_connectionTasks.TryGetValue(printer.Id, out var task))
            {
                task = Task.Run(() =>
                {
                    var errorCode = ReaPi.ConnectWaitB(printer.Settings.ConnectionString, out connectionId);

                    if (errorCode != ReaPi.EErrorCode.OK)
                        throw new ReaPiException($"Failed to connect BrandPrinter at {printer.Settings.ConnectionString}: {errorCode}");

                    return connectionId;
                });
            }

            return task;
        }
    }
}