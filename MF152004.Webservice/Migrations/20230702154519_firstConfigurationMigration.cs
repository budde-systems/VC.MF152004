using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MF152004.Webservice.Migrations
{
    /// <inheritdoc />
    public partial class firstConfigurationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BradingPdfCongigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoxBarcodeReference = table.Column<string>(type: "TEXT", nullable: true),
                    ClientReference = table.Column<string>(type: "TEXT", nullable: true),
                    BrandingPdfReference = table.Column<string>(type: "TEXT", nullable: true),
                    ConfigurationInUse = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BradingPdfCongigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabelPrinterConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoxBarcodeReference = table.Column<string>(type: "TEXT", nullable: true),
                    LabelPrinterReference = table.Column<string>(type: "TEXT", nullable: true),
                    ConfigurationInUse = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelPrinterConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SealerRoutesConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoxBarcodeReference = table.Column<string>(type: "TEXT", nullable: true),
                    SealerRouteReference = table.Column<string>(type: "TEXT", nullable: true),
                    ConfigurationInUse = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SealerRoutesConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeightToleranceConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeigthTolerance = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightToleranceConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BradingPdfCongigs");

            migrationBuilder.DropTable(
                name: "LabelPrinterConfigs");

            migrationBuilder.DropTable(
                name: "SealerRoutesConfigs");

            migrationBuilder.DropTable(
                name: "WeightToleranceConfigs");
        }
    }
}
