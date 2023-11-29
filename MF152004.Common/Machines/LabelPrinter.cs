using BlueApps.MaterialFlow.Common.Machines;
using BlueApps.MaterialFlow.Common.Models.Machines;
using System.Net;
using System.Net.Sockets;

namespace MF152004.Common.Machines;

public class LabelPrinter : IMachine
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    
    public string BasePosition { get; set; } = string.Empty;
    
    public string SubPosition { get; set; } = string.Empty;

    public string IP { get; set; } = string.Empty;
    
    public int Port { get; set; }
    
    public Scanner? RelatedScanner { get; set; }
    
    public List<int> TracedPackets { get; set; } = new();

    /// <summary>
    /// Print async without waiting
    /// </summary>
    /// <param name="data"></param>
    /// <exception cref="Exception"></exception>
    public async void Print(byte[] data)
    {
        if (string.IsNullOrWhiteSpace(IP) || Port < 1)
            throw new Exception("IP or port has wrong value"); //TODO: own exception

        using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.NoDelay = true;

        await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(IP), Port));
        await client.SendAsync(data);
        client.Close();
    }
}