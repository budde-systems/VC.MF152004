using BlueApps.MaterialFlow.Common.Models;

namespace MF152004.Models.EventArgs;

public class UpdateDestinationsEventArgs : System.EventArgs
{
    public List<Destination> UpdatedDestinations { get; set; } = new();
}