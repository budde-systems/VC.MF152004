using BlueApps.MaterialFlow.Common.Models;
using MF152004.Models.Configurations;
using MF152004.Models.Main;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MF152004.Webservice.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<SealerRoute> SealerRoutesConfigs { get; set; }
        public DbSet<BrandingPdf> BradingPdfCongigs { get; set; }
        public DbSet<LabelPrinter> LabelPrinterConfigs { get; set; }
        public DbSet<WeightTolerance> WeightToleranceConfigs { get; set; }
        public DbSet<Destination> Destinations { get; set; }
        public DbSet<Carrier> Carriers { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<ClientReference> ClientReferences { get; set; }
        public DbSet<DeliveryService> DeliveryServices { get; set; }
        public DbSet<NoRead> NoReads { get; set; }
        public DbSet<Scan> WeightScans { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)  
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            DateTimeConversion(modelBuilder);

            base.OnModelCreating(modelBuilder);
            DefaultDestinations(modelBuilder);
        }

        private void DateTimeConversion(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.ReceivedAt)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.BoxBrandedAt_1)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.BoxBrandedAt_1)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.LabelPrintedAt)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.LabelPrintingFailedAt)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.LeftSealerAt)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.LeftErrorAisleAt)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.DestinationRouteReferenceUpdatedAt)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
            
            modelBuilder
                .Entity<Shipment>()
                .Property(e => e.DestinationReachedAt)
                .HasConversion(
                    v => v.Value.ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK"), v => DateTime.Parse(v, CultureInfo.InvariantCulture)
                );
        }

        private void DefaultDestinations(ModelBuilder builder)
        {
            builder.Entity<Destination>()
                .HasData(
                new Destination
                {
                    Id = 1,
                    Name = "Tor 1",
                    UI_Id = "gate1"
                },
                new Destination
                {
                    Id = 2,
                    Name = "Tor 2",
                    UI_Id = "gate2"
                },
                new Destination
                {
                    Id = 3,
                    Name = "Tor 3",
                    UI_Id = "gate3"
                },
                new Destination
                {
                    Id = 4,
                    Name = "Tor 4",
                    UI_Id = "gate4"
                },
                new Destination
                {
                    Id = 5,
                    Name = "Tor 5",
                    UI_Id = "gate5"
                },
                new Destination
                {
                    Id = 6,
                    Name = "Tor 6",
                    UI_Id = "gate6"
                },
                new Destination
                {
                    Id = 7,
                    Name = "Tor 7",
                    UI_Id = "gate7"
                },
                new Destination
                {
                    Id = 8,
                    Name = "Tor 8",
                    UI_Id = "gate8"
                },
                new Destination
                {
                    Id = 9,
                    Name = "Tor 9",
                    UI_Id = "gate9"
                },
                new Destination
                {
                    Id = 10,
                    Name = "Tor 10",
                    UI_Id = "gate10"
                },
                new Destination
                {
                    Id = 11,
                    Name = "Fehlerinsel",
                    UI_Id = "faultisland"
                });
        }
    }
}
