using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MF152004.Webservice.Migrations
{
    /// <inheritdoc />
    public partial class updatedCCCByActiveProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "DeliveryServices",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Countries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "ClientReferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Carriers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "DeliveryServices");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "ClientReferences");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "Carriers");
        }
    }
}
