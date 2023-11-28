using BlueApps.MaterialFlow.Common.Connection.Packets.Events;
using BlueApps.MaterialFlow.Common.Models;
using BlueApps.MaterialFlow.Common.Values.Types;
using MF152004.Webservice.Data;
using Microsoft.EntityFrameworkCore;

namespace MF152004.Webservice.Services
{
    public class GeneralPacketService
    {
        private readonly ILogger<GeneralPacketService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly MessageDistributorService _messageDistributorService;

        public GeneralPacketService(IServiceProvider service, MessageDistributorService messageDistributorService)
        {
            _messageDistributorService = messageDistributorService;

            var scope = service.CreateScope();

            _logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<GeneralPacketService>();
            _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _messageDistributorService.GeneralPacketReceived += OnGeneralPacketReceive;
            _logger.LogInformation("The general packet service has been startet successfully");

            RemoveOldEntries();
        }

        private async void RemoveOldEntries()
        {
            var oldNoReads = await _context.NoReads
                .Where(_ => _.AtTime < DateTime.Now.AddYears(-2))
                .ToListAsync();

            if (oldNoReads.Any())
            {
                try
                {
                    _context.NoReads.RemoveRange(oldNoReads);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"{oldNoReads.Count} no read entities has been removed from DB successfully");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception.ToString());
                }
            }
        }

        private void OnGeneralPacketReceive(object? sender, GeneralPacketEventArgs packet)
        {
            if (packet is null)
            {
                _logger.LogWarning("The event-argument is null");
                return;
            }

            if (packet.GeneralPacket is null)
            {
                _logger.LogWarning("The general packet of event-argument is null");
                return;
            }

            if (packet.GeneralPacket.PacketContextes.Any(_ => _ == GeneralPacketContext.NoRead))
            {
                AddNoReads(packet.GeneralPacket.NoReads?.ToArray());
            }
        }

        public async void AddNoReads(params NoRead[]? noReads)
        {
            if (noReads is null)
            {
                _logger.LogWarning("No Read object is null");
                return;
            }

            foreach (var noRead in noReads)
                _context.NoReads.Add(noRead);

            await _context.SaveChangesAsync();
        }

        //TODO: methods about reading no reads for statistics
    }
}
