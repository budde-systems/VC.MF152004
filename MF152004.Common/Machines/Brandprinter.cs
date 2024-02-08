using MF152004.Models.Settings.BrandPrinter;
using ReaPiSharp;

namespace MF152004.Common.Machines;

public class BrandPrinter
{
    private int _jobId;
    private readonly object _connectionLock = new();
    private Task<ReaPi.ConnectionIdentifier>? _connectionTask;

    public string Name { get; set; } = null!;

    public BrandPrinterSettings Settings { get; set; } = new();

    private Task<ReaPi.ConnectionIdentifier> ConnectAsync()
    {
        lock (_connectionLock)
        {
            return _connectionTask ??= Task.Run(() =>
            {
                try
                {
                    var errorCode = ReaPi.ConnectWaitB(Settings.ConnectionString, out var connectionId);

                    if (errorCode != ReaPi.EErrorCode.OK)
                        throw new ReaPiException($"Failed to connect BrandPrinter at {Settings.ConnectionString}: {errorCode}");

                    return connectionId;
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

    public async Task Print(string value)
    {
        var connection = await ConnectAsync().ConfigureAwait(false);

        await Task.Run(() =>
        {
            try
            {
                var jobId = Interlocked.Increment(ref _jobId);

                var response = ReaPi.SetJob(connection, jobId, Settings.Configuration.Job);
                if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"SetJob failed: {this}, {value}: {ReaPi.GetErrorMessage(response, out _)}");

                var labelContent = ReaPi.CreateLabelContent();

                var error = ReaPi.PrepareLabelContent(labelContent, jobId, Settings.Configuration.Group, Settings.Configuration.Object, Settings.Configuration.Content, value);
                if (error != ReaPi.EErrorCode.OK) throw new ReaPiException($"PrepareLabelContent failed: {this}, {value}: {error}");

                response = ReaPi.SetLabelContent(connection, labelContent);
                if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"SetLabelContent failed: {this}, {value}: {ReaPi.GetErrorMessage(response, out _)}");

                response = ReaPi.StartJob(connection, jobId);
                if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"StartJob failed: {this}, {value}: {ReaPi.GetErrorMessage(response, out _)}");
            }
            catch
            {
                lock (_connectionLock) _connectionTask = null;
                throw;
            }
        });
    }
}