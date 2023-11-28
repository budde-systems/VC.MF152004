﻿// <auto-generated />
using System;
using MF152004.Webservice.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MF152004.Webservice.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20230717133829_migratedDestination")]
    partial class migratedDestination
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.8");

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.Carrier", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DestinationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DestinationId");

                    b.ToTable("Carriers");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.ClientReference", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DestinationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DestinationId");

                    b.ToTable("ClientReferences");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.Country", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DestinationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DestinationId");

                    b.ToTable("Countries");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.DeliveryService", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DestinationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DestinationId");

                    b.ToTable("DeliveryServices");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.Destination", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Active")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Destinations");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Active = false,
                            Name = "Tor 1"
                        },
                        new
                        {
                            Id = 2,
                            Active = false,
                            Name = "Tor 2"
                        },
                        new
                        {
                            Id = 3,
                            Active = false,
                            Name = "Tor 3"
                        },
                        new
                        {
                            Id = 4,
                            Active = false,
                            Name = "Tor 4"
                        },
                        new
                        {
                            Id = 5,
                            Active = false,
                            Name = "Tor 5"
                        },
                        new
                        {
                            Id = 6,
                            Active = false,
                            Name = "Tor 6"
                        },
                        new
                        {
                            Id = 7,
                            Active = false,
                            Name = "Tor 7"
                        },
                        new
                        {
                            Id = 8,
                            Active = false,
                            Name = "Tor 8"
                        },
                        new
                        {
                            Id = 9,
                            Active = false,
                            Name = "Tor 9"
                        },
                        new
                        {
                            Id = 10,
                            Active = false,
                            Name = "Tor 10"
                        },
                        new
                        {
                            Id = 11,
                            Active = false,
                            Name = "Fehlerinsel"
                        });
                });

            modelBuilder.Entity("MF152004.Models.Configurations.BrandingPdf", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BoxBarcodeReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "box_barcode_reference");

                    b.Property<string>("BrandingPdfReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "branding_pdf_reference");

                    b.Property<string>("ClientReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "client_reference");

                    b.Property<bool>("ConfigurationInUse")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("BradingPdfCongigs");
                });

            modelBuilder.Entity("MF152004.Models.Configurations.LabelPrinter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BoxBarcodeReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "box_barcode_reference");

                    b.Property<bool>("ConfigurationInUse")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LabelPrinterReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "label_printer_reference");

                    b.HasKey("Id");

                    b.ToTable("LabelPrinterConfigs");
                });

            modelBuilder.Entity("MF152004.Models.Configurations.SealerRoute", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BoxBarcodeReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "box_barcode_reference");

                    b.Property<bool>("ConfigurationInUse")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SealerRouteReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "sealer_route_reference");

                    b.HasKey("Id");

                    b.ToTable("SealerRoutesConfigs");
                });

            modelBuilder.Entity("MF152004.Models.Configurations.WeightTolerance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ConfigurationInUse")
                        .HasColumnType("INTEGER");

                    b.Property<double>("WeigthTolerance")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("WeightToleranceConfigs");
                });

            modelBuilder.Entity("MF152004.Models.Main.Shipment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasAnnotation("Relational:JsonPropertyName", "shipment_id");

                    b.Property<string>("BoxBarcodeReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "box_barcode_reference");

                    b.Property<DateTime>("BoxBrandedAt_1")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "box_branded_at_1");

                    b.Property<DateTime>("BoxBrandedAt_2")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "box_branded_at_2");

                    b.Property<string>("Carrier")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "carrier");

                    b.Property<string>("ClientReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "client_reference");

                    b.Property<string>("Country")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "country");

                    b.Property<string>("DestinationRouteReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "destination_route_reference");

                    b.Property<DateTime>("DestinationRouteReferenceUpdatedAt")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "destination_route_reference_updated_at");

                    b.Property<DateTime>("LabelPrintedAt")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "label_printed_at");

                    b.Property<DateTime>("LabelPrintingFailedAt")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "label_printing_failed_at");

                    b.Property<DateTime>("LeftSealerAt")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "left_sealer_at");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "conveyor_belt_message");

                    b.Property<DateTime>("ReceivedOn")
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "status");

                    b.Property<string>("TrackingCode")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "tracking_code");

                    b.Property<string>("TransportationReference")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "transportation_reference");

                    b.Property<double>("Weight")
                        .HasColumnType("REAL")
                        .HasAnnotation("Relational:JsonPropertyName", "weight");

                    b.HasKey("Id");

                    b.ToTable("Shipments");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.Carrier", b =>
                {
                    b.HasOne("BlueApps.MaterialFlow.Common.Models.Destination", null)
                        .WithMany("Carriers")
                        .HasForeignKey("DestinationId");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.ClientReference", b =>
                {
                    b.HasOne("BlueApps.MaterialFlow.Common.Models.Destination", null)
                        .WithMany("ClientReferences")
                        .HasForeignKey("DestinationId");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.Country", b =>
                {
                    b.HasOne("BlueApps.MaterialFlow.Common.Models.Destination", null)
                        .WithMany("Countrys")
                        .HasForeignKey("DestinationId");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.DeliveryService", b =>
                {
                    b.HasOne("BlueApps.MaterialFlow.Common.Models.Destination", null)
                        .WithMany("DeliveryServices")
                        .HasForeignKey("DestinationId");
                });

            modelBuilder.Entity("BlueApps.MaterialFlow.Common.Models.Destination", b =>
                {
                    b.Navigation("Carriers");

                    b.Navigation("ClientReferences");

                    b.Navigation("Countrys");

                    b.Navigation("DeliveryServices");
                });
#pragma warning restore 612, 618
        }
    }
}
