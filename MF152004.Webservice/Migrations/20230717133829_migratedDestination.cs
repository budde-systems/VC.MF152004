using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MF152004.Webservice.Migrations
{
    /// <inheritdoc />
    public partial class migratedDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Active = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carriers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carriers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carriers_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClientReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientReferences_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Countries_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliveryServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryServices_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Destinations",
                columns: new[] { "Id", "Active", "Name" },
                values: new object[,]
                {
                    { 1, false, "Tor 1" },
                    { 2, false, "Tor 2" },
                    { 3, false, "Tor 3" },
                    { 4, false, "Tor 4" },
                    { 5, false, "Tor 5" },
                    { 6, false, "Tor 6" },
                    { 7, false, "Tor 7" },
                    { 8, false, "Tor 8" },
                    { 9, false, "Tor 9" },
                    { 10, false, "Tor 10" },
                    { 11, false, "Fehlerinsel" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carriers_DestinationId",
                table: "Carriers",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientReferences_DestinationId",
                table: "ClientReferences",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_DestinationId",
                table: "Countries",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryServices_DestinationId",
                table: "DeliveryServices",
                column: "DestinationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Carriers");

            migrationBuilder.DropTable(
                name: "ClientReferences");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "DeliveryServices");

            migrationBuilder.DropTable(
                name: "Destinations");
        }
    }
}
