using BlueApps.MaterialFlow.Common.Values.Types;

namespace MF152004.Workerservice.Common;

public static class CommonData
{
    public static Dictionary<TopicType, string> Topics { get; set; }
    
    public static double WeightTolerance { get; set; } = 0;
    
    public static string NoRead { get; internal set; } = "NOREAD";
    
    public static string FaultIsland { get; set; } = "Fehlerinsel";
    
    public static string LabelprinterNoMatchMsg { get; set; } = "keine Vorgehensweise hinterlegt";
}