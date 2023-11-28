using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MF152004.Webservice.Migrations
{
    /// <inheritdoc />
    public partial class changedDestinationReachedProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationReached",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "ReceivedOn",
                table: "Shipments",
                newName: "ReceivedAt");

            migrationBuilder.AddColumn<string>(
                name: "DestinationReachedAt",
                table: "Shipments",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationReachedAt",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                table: "Shipments",
                newName: "ReceivedOn");

            migrationBuilder.AddColumn<bool>(
                name: "DestinationReached",
                table: "Shipments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
