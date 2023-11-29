using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Models.Values.Types;

namespace MF152004.Webservice.Common;

public static class CommonData
{
    public static Dictionary<TopicType, string> Topics { get; set; }

    public static Dictionary<API_Endpoint, string> Endpoints { get; set; }
}