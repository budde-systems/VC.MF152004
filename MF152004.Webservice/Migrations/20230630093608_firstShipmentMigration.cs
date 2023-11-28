using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MF152004.Webservice.Migrations
{
    /// <inheritdoc />
    public partial class firstShipmentMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientReference = table.Column<string>(type: "TEXT", nullable: true),
                    BoxBarcodeReference = table.Column<string>(type: "TEXT", nullable: true),
                    TransportationReference = table.Column<string>(type: "TEXT", nullable: true),
                    TrackingCode = table.Column<string>(type: "TEXT", nullable: true),
                    Carrier = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    BoxBrandedAt_1 = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BoxBrandedAt_2 = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LabelPrintedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LabelPrintingFailedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DestinationRouteReference = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationRouteReferenceUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Shipments");
        }
    }
}
