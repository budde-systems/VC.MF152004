using MF152004.Models.Settings.BrandPrinter;
using ReaPiSharp;

namespace MF152004.Common.Machines;

public class BrandPrinter
{
    private readonly object _connectionLock = new();
    private Task<ReaPi.ConnectionIdentifier>? _connectionTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private ReaPi.LabelContentHandle _labelContent;

    public string Name { get; set; } = null!;

    public override string ToString() => Name;

    public BrandPrinterSettings Settings { get; set; } = new();

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
                        var errorCode = ReaPi.ConnectWaitB(Settings.ConnectionString, out var connection);

                        if (errorCode != ReaPi.EErrorCode.OK)
                            throw new ReaPiException($"Failed to connect BrandPrinter at {Settings.ConnectionString}: {errorCode}");

                        var response = ReaPi.SetJob(connection, 1, Settings.Configuration.Job);
                        if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"{this}: Initial SetJob failed: {ReaPi.GetErrorMessage(response, out _)}");

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