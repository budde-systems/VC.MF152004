using BlueApps.MaterialFlow.Common.Connection.PacketHelper;
using BlueApps.MaterialFlow.Common.Machines.BaseMachines;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Workerservice.Common;
using MF152004.Workerservice.Connection.Packets.Settings;

namespace MF152004.Workerservice.Connection.Packets.PacketHelpers;

public class PLC152004_PacketHelper : PLC_MessagePacketHelper
{
    public PLC152004_PacketHelper() : base(new PacketSettings())
    {
        InTopic = CommonData.Topics[TopicType.PLC_Workerservice];
        OutTopic = CommonData.Topics[TopicType.Workerservice_PLC];
    }

    /// <summary>
    /// C2
    /// </summary>
    /// <param name="diverter"></param>
    /// <param name="packetTracing"></param>
    public void Create_FlowSortPosition(IDiverter? diverter, int packetTracing)
    {
        if (diverter is null)
            return;

        ClearAreas();
        CreatePacketId();
        Command = PLC_Command.C002;
        Areas[2] = packetTracing.ToString();
        Areas[3] = ((byte)diverter.DriveDirection).ToString();
        Areas[4] = diverter.BasePosition;
    }

    /// <summary>
    /// C2
    /// </summary>
    /// <param name="diverter"></param>
    /// <param name="packetTracing"></param>
    public void Create_NoExitFlowSortPosition(IDiverter? diverter, int packetTracing)
    {
        if (diverter is null)
            return;

        ClearAreas();
        CreatePacketId();
        Command = PLC_Command.C002;
        Areas[2] = packetTracing.ToString();
        Areas[3] = ((byte)diverter.DriveDirection).ToString();            
    }

    public void Create_StopAndGo(string basePosition, bool go)
    {
        ClearAreas();
        CreatePacketId();
        Command = PLC_Command.C009;
        Areas[3] = go ? "1" : "0";
        Areas[4] = string.IsNullOrEmpty(basePosition) ? string.Empty : basePosition;
    }

    public void Create_LabelPrinter(string labelPrinterRef)
    {
        ClearAreas();
        CreatePacketId();
        Command = PLC_Command.C008;
        Areas[3] = labelPrinterRef;
    }
}