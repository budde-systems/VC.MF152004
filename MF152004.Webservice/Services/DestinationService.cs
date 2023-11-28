using BlueApps.MaterialFlow.Common.Connection.Client;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Common.Connection.Packets.PacketHelpers;
using MF152004.Models.Connection.Packets.HubPacket;
using MF152004.Webservice.Common;
using MF152004.Webservice.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MF152004.Webservice.Services
{
    public class DestinationService
    {
        private static ConcurrentDictionary<int, Destination>? _cachedDestinations;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<DestinationService> _logger;
        private readonly MqttClient _client;

        public DestinationService(ApplicationDbContext context, ILogger<DestinationService> logger, MqttClient client)
        {
            _context = context;
            _logger = logger;
            _client = client;

            _cachedDestinations = new ConcurrentDictionary<int, Destination>(_context.Destinations
                .Include(_ => _.Carriers)
                .Include(_ => _.Countries)
                .Include(_ => _.DeliveryServices)
                .Include(_ => _.ClientReferences)
                .AsQueryable()
                //.AsNoTracking() <= TODO´: Einfügen und testen
                .ToDictionary(d => d.Id));
        }

        public async void UpdateDestination(Destination? destination)
        {
            if (destination is null)
                return;

            try
            {
                _context.Entry(destination).State = EntityState.Detached; //TODO: not clean solution

                _context.Destinations.Update(destination);
                await _context.SaveChangesAsync();

                if (_cachedDestinations != null)
                    _cachedDestinations.AddOrUpdate(destination.Id, destination, UpdateCache);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.ToString());
            }
        }

        private Destination UpdateCache(int id, Destination destination)
        {
            Destination? old;

            if (_cachedDestinations != null)
            {
                if (_cachedDestinations.TryGetValue(id, out old))
                {
                    if (_cachedDestinations.TryUpdate(id, destination, old))
                    {
                        return destination;
                    }
                }
            }

            return null!;
        }

        public List<Destination> Destinations
        {
            get
            {
                if (_cachedDestinations == null)
                    return new();
                else
                    return _cachedDestinations.Values.ToList();
            }
        }

        public async Task<Destination?> GetDestinationAsync(int? id)
        {
            var destination = await _context.Destinations
                .Include(_ => _.Carriers)
                .Include(_ => _.Countries)
                .Include(_ => _.DeliveryServices)
                .Include(_ => _.ClientReferences)
                .SingleOrDefaultAsync(_ => _.Id == id);

            return destination;
        }

        public async Task<Destination?> GetDestinationAsync(string? name)
        {
            var destination = await _context.Destinations
                .Include(_ => _.Carriers)
                .Include(_ => _.Countries)
                .Include(_ => _.DeliveryServices)
                .Include(_ => _.ClientReferences)
                .AsSplitQuery()
                .SingleOrDefaultAsync(_ => _.Name == name);

            return destination;
        }

        public async Task<Destination> GetFaultDestinationAsync() =>
            (await _context.Destinations
            .SingleOrDefaultAsync(_ => _.Name == "Fehlerinsel")) ?? new Destination() { Name = "Fehlerinsel" };
        
        public Destination GetFaultDestination() =>
            Destinations.SingleOrDefault(_ => _.Name == "Fehlerinsel") ?? new Destination() { Name = "Fehlerinsel" };


        /// <summary>
        /// Get all matched destinations as one string, divided by [;]
        /// </summary>
        /// <param name="carrier"></param>
        /// <param name="country"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public string? GetDestinationNames(string? carrier, string? country, string? clientId)
        {
            if (FailValidation(carrier, country, clientId))
            {
                _logger.LogWarning("The validation of carrier/country/client ID has been failed");
                return GetFaultDestination()?.Name;
            }

            if (Destinations is null)
            {
                _logger.LogWarning("Destinations could not be found in DB");
                return GetFaultDestination()?.Name;
            }

            var destinations = ValidateAndGetRequiredDestinations(carrier, country, clientId);

            var destinationNames = destinations
                .Where(d => d.Carriers
                .Any(c => c.Name.ToLower() == carrier.ToLower() && c.Active) && d.Countries
                .Any(c => c.Name.ToLower() == country.ToLower() && c.Active) && d.ClientReferences
                .Any(c => c.Name.ToLower() == clientId.ToLower() && c.Active))?.Select(_ => _.Name);

            return destinationNames != null ? string.Join(";", destinationNames) : GetFaultDestination()?.Name;
        }

        private List<Destination> ValidateAndGetRequiredDestinations(string? carrier, string? country, string? clientId)
        {
            var destinations = Destinations
                .Where(d => d.Carriers != null && d.Carriers
                .Select(car => car.Name.ToLower()).Contains(carrier.ToLower()))
                .ToList();

            if (!destinations.Any())
            {
                _logger.LogWarning($"No carrier could be found for shipments carrier {carrier}");                
            }
            else
            {
                destinations = destinations
                .Where(d => d.Countries != null && d.Countries
                .Select(c => c.Name.ToLower()).Contains(country.ToLower()))
                .ToList();

                if (!destinations.Any())
                {
                    _logger.LogWarning($"No country could be found for shipments country {country}");                    
                }
                else
                {
                    destinations = destinations
                        .Where(d => d.ClientReferences != null && d.ClientReferences
                        .Select(client => client.Name.ToLower()).Contains(clientId.ToLower()))
                        .ToList();

                    if (!destinations.Any())
                    {
                        _logger.LogWarning($"No client reference could be found for shipments client ID {clientId}");                        
                    }
                }
            }

            return destinations;
        }

        private bool FailValidation(string? carrier, string? country, string? clientId) =>
            string.IsNullOrEmpty(carrier) && string.IsNullOrEmpty(country) && string.IsNullOrEmpty(clientId);

        public async Task<List<Destination>> GetDestinationsAsync()
        {
            if (_context.Destinations is null)
                return new List<Destination>();

            var destinations = await _context.Destinations
                .Include(_ => _.Carriers)
                .Include(_ => _.Countries)
                .Include(_ => _.DeliveryServices)
                .Include(_ => _.ClientReferences)
                .AsQueryable()
                .ToListAsync();

            return destinations;
        }

        internal void OnNewDestinationStatus(DestinationStatus status)
        {
            if (Destinations is null)
            {
                _logger.LogWarning("Status of destinations can't be updated. The cached " +
                    "destinations is null.");
                return;
            }
            else if(status is null || status.Destinations is null)
            {
                _logger.LogWarning("Status of destinations can't be updated. " +
                    "The status or status.destinations is null.");
                return;
            }

            foreach (var dest in status.Destinations)
            {
                var storedDestination = Destinations.FirstOrDefault(d => d.Id == dest.Id);

                if (storedDestination != null && storedDestination.Active != dest.Active) //better performance
                {
                    storedDestination.Active = dest.Active;
                    UpdateDestination(storedDestination); 
                }
            }
        }
    }
}
