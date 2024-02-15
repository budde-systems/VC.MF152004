using BlueApps.MaterialFlow.Common.Sectors;
using MF152004.Models.Settings.BrandPrinter;
using Microsoft.Extensions.Logging;
using ReaPiSharp;

namespace MF152004.Common.Machines;

public class BrandPrinter
{
    private readonly object _connectionLock = new();
    private Task<ReaPi.ConnectionIdentifier>? _connectionTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private ReaPi.LabelContentHandle _labelContent;
    private readonly ILogger<Sector> _logger;
    private string? _currentValue;

    public BrandPrinter(ILogger<Sector> logger) => _logger = logger;

    public string Name { get; set; } = null!;

    public override string ToString() => Name;

    public BrandPrinterSettings Settings { get; init; } = new();

    private Task<ReaPi.ConnectionIdentifier> ConnectAsync()
    {
        lock (_connectionLock)
        {
            return _connectionTask ??= Task.Run(async () =>
            {
                try
                {
                    await _semaphore.WaitAsync();

                    try
                    {
                        _logger.LogInformation("Connecting {0} at {1}...", this, Settings.ConnectionString);
                        var errorCode = ReaPi.ConnectWaitB(Settings.ConnectionString, out var connection);

                        if (errorCode != ReaPi.EErrorCode.OK)
                            throw new ReaPiException($"Failed to connect {this} at {Settings.ConnectionString}: {errorCode}");

                        var response = ReaPi.SetJob(connection, 1, Settings.Configuration.Job);
                        if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"{this}: Initial SetJob failed: {ReaPi.GetErrorMessage(response, out _)}");

                        // We are trying to stop the existing job, if any. Ignoring the result
                        response = ReaPi.StopJob(connection, 1);
                        ReaPi.GetErrorCode(response, out _);

                        response = ReaPi.StartJob(connection, 1);
                        if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"{this}: StartJob failed: {ReaPi.GetErrorMessage(response, out _)}");

                        _labelContent = ReaPi.CreateLabelContent();

                        UpdateLabel(connection, Settings.Configuration.NoPrintValue);

                        return connection;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                catch
                {
                    lock (_connectionLock)
                    {
                        _connectionTask = null;
                        throw;
                    }
                }
            });
        }
    }

    private void UpdateLabel(ReaPi.ConnectionIdentifier connection, string value)
    {
        var error = ReaPi.PrepareLabelContent(_labelContent, 1, Settings.Configuration.Group, Settings.Configuration.Object, Settings.Configuration.Content, value);
        if (error != ReaPi.EErrorCode.OK) throw new ReaPiException($"{this}: PrepareLabelContent failed ({value}): {error}");

        var response = ReaPi.SetLabelContent(connection, _labelContent);
        if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"{this}: SetLabelContent failed ({value}): {ReaPi.GetErrorMessage(response, out _)}");
        
        _currentValue = value;
    }

    public async Task Print(string value)
    {
        var connection = await ConnectAsync().ConfigureAwait(false);

        await Task.Run(async () =>
        {
            try
            {
                await _semaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    UpdateLabel(connection, value);

                    // Resetting the printer to default value after timeout
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(Settings.Configuration.ResetTimeout);

                            if (_currentValue != Settings.Configuration.NoPrintValue)
                            {
                                _logger.LogInformation("{0}: Resetting ref to default {1}", this, Settings.Configuration.NoPrintValue);
                                await Print(Settings.Configuration.NoPrintValue);
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    });
                }
                finally 
                { 
                    _semaphore.Release(); 
                }
            }
            catch
            {
                lock (_connectionLock) _connectionTask = null;
                throw;
            }
        });
    }
}