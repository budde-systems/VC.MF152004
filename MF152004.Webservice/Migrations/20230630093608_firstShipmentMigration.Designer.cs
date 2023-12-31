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
    [Migration("20230630093608_firstShipmentMigration")]
    partial class firstShipmentMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.8");

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

                    b.Property<string>("Message")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "conveyor_belt_message");

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
#pragma warning restore 612, 618
        }
    }
}
