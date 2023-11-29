using MF152004.Models.Main;

namespace MF152004.Models.EventArgs
{
    public class NewShipmentEventArgs : System.EventArgs
    {
        public List<Shipment>? NewShipments { get; set; }
    }
}
