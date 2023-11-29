using MF152004.Models.Main;

namespace MF152004.Models.EventArgs
{
    public class UpdateShipmentEventArgs : System.EventArgs
    {
        public List<Shipment>? UpdatedShipments { get; set; }
    }
}
