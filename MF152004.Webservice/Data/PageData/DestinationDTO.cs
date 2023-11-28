using BlueApps.MaterialFlow.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MF152004.Webservice.Data.PageData
{
    public class DestinationDTO : Destination
    {
        public Carrier SelectedCarrier { get; set; }
        public int[] SelectedCountries { get; set; }
        public int[] SelectedClientIds { get; set; }
        public int GateId { get; set; }

        public DestinationDTO(Destination destination)
        {
            Carriers = destination.Carriers;
            Countries = destination.Countries;
            ClientReferences = destination.ClientReferences;
            Active = destination.Active;
            DeliveryServices = destination.DeliveryServices;
            Name = destination.Name;

            GateId = destination.Id;

            SelectedCarrier = Carriers.FirstOrDefault(x => x.Active);
            SelectedCountries = Countries.Where(_ => _.Active).Select(_ => _.Id).ToArray();
            SelectedClientIds = ClientReferences.Where(_ => _.Active).Select(_ => _.Id).ToArray();
        }

        public List<SelectListItem> GetCarriersSelectList()
        {
            if (Carriers == null || !Carriers.Any())
            {
                return GetDefaultList();
            }
            else
            {
                var list = new List<SelectListItem>();
                list.AddRange(Carriers.Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() }));
                return list;
            }
        }

        public List<SelectListItem> GetCountriesSelectList()
        {
            if (Countries is null || !Countries.Any())
            {
                return GetDefaultList();
            }
            else
            {
                var list = new List<SelectListItem>();
                list.AddRange(Countries.Select(x => new SelectListItem(x.Name, x.Id.ToString())));
                return list;
            }
        }

        public List<SelectListItem> GetClientReferencesSelectList()
        {
            if (ClientReferences is null || !ClientReferences.Any())
            {
                return GetDefaultList();
            }
            else
            {
                var list = new List<SelectListItem>();
                list.AddRange(ClientReferences.Select(x => new SelectListItem(x.Name, x.Id.ToString())));
                return list;
            }
        }

        private List<SelectListItem> GetDefaultList() =>
            new List<SelectListItem>() { new SelectListItem("keine Daten vorhanden", "0") };
}
}
