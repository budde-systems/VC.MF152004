﻿using ReaPiSharp;

namespace BrandprinterTest;

public class BrandPrinterConfig
{
    public string ConnectionString { get; set; }

    public string? JobFile { get; set; }
    public string? Group { get; set; }
    public string? Object { get; set; }
    public string? Content { get; set; }
    public string? Value { get; set; }
    public string? NoPrintValue { get; set; }
}


public class BrandPrinter
{
    public BrandPrinterConfig Settings { get; } = new();

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string BasePosition { get; set; }
    public string SubPosition { get; set; }

    public override string ToString() => $"BrandPrinter {Name}";
}

public class ReaPiException(string? message) : Exception(message);

public class BrandPrinterHub()
{
    private readonly object _connectionLock = new();
    private readonly Dictionary<string, Task<ReaPi.ConnectionIdentifier>> _connectionTasks = new();
    private readonly Dictionary<string, ReaPi.ConnectionIdentifier> _connections = new();
    private int _jobId;

    public async Task Print(BrandPrinter printer, string value)
    {
        var connection = await ConnectAsync(printer).ConfigureAwait(false);

        await Task.Run(() =>
        {

        });
        try
        {
            var jobId = Interlocked.Increment(ref _jobId);
            
            var response = ReaPi.SetJob(connection, jobId, printer.Settings.JobFile);
            if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"SetJob failed: {printer}, {value}: {ReaPi.GetErrorMessage(response, out _)}");

            var labelContent = ReaPi.CreateLabelContent();
            
            var error = ReaPi.PrepareLabelContent(labelContent, jobId, printer.Settings.Group, printer.Settings.Object, printer.Settings.Content, value);
            if (error != ReaPi.EErrorCode.OK) throw new ReaPiException($"PrepareLabelContent failed: {printer}, {value}: {error}");
            
            response = ReaPi.SetLabelContent(connection, labelContent);
            if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"SetLabelContent failed: {printer}, {value}: {ReaPi.GetErrorMessage(response, out _)}");

            response = ReaPi.StartJob(connection, jobId);
            if (ReaPi.GetErrorCode(response, out _) != 0) throw new ReaPiException($"StartJob failed: {printer}, {value}: {ReaPi.GetErrorMessage(response, out _)}");
        }
        catch
        {
            lock (_connectionLock) _connections.Remove(printer.Id);
            throw;
        }
    }
    
    private Task<ReaPi.ConnectionIdentifier> ConnectAsync(BrandPrinter printer)
    {
        lock (_connectionLock)
        {
            if (_connections.TryGetValue(printer.Id, out var connectionId)) 
                return Task.FromResult(connectionId);

            if (!_connectionTasks.TryGetValue(printer.Id, out var task))
            {
                _connectionTasks.Add(printer.Id, task = Task.Run(() =>
                {
                    try
                    {
                        var errorCode = ReaPi.ConnectWaitB(printer.Settings.ConnectionString, out connectionId);

                        if (errorCode != ReaPi.EErrorCode.OK)
                            throw new ReaPiException($"Failed to connect BrandPrinter at {printer.Settings.ConnectionString}: {errorCode}");

                        lock (_connectionLock)
                        {
                            _connectionTasks.Remove(printer.Id);
                            _connections.Add(printer.Id, connectionId);
                            return connectionId;
                        }
                    }
                    catch
                    {
                        lock (_connectionLock)
                        {
                            _connectionTasks.Remove(printer.Id);
                            throw;
                        }
                    }
                }));
            }

            return task;
        }
    }
}