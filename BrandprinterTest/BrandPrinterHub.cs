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

public class BrandPrinterHub
{
    private readonly ILogger<BrandPrinterHub> _logger;

    private readonly ReaPi.connectionCallbackPtr _connectionCallback;
    private readonly ReaPi.eventCallbackPtr _eventCallback;
    private readonly ReaPi.responseCallbackPtr _responseCallback;

    public BrandPrinterHub(ILogger<BrandPrinterHub> logger)
    {
        _logger = logger;

        //_connectionCallback = OnConnectionCallback;
        //_eventCallback = OnEventCallback;
        //_responseCallback = OnResponseCallback;

        //ReaPi.RegisterConnectionCallback(_connectionCallback, 0);
    }

    public Task ConnectAsync(BrandPrinter printer)
    {
        return Task.Run(() =>
        {
            var connectionId = ReaPi.ConnectWait(printer.Settings.ConnectionString);

            if (connectionId < 0)
                throw new ReaPiException($"Failed to connect BrandPrinter at {printer.Settings.ConnectionString}: {connectionId}");

            ReaPi.GetNetworkConfig(connectionId);
        });
    }
}