using MF152004.Models.Main;
using Microsoft.EntityFrameworkCore;

namespace MF152004.Webservice.ComTest.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Shipment> Shipments { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }
    }
}
