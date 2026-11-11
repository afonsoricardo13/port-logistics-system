using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.AddColumn<double>(
                name: "AverageSpeedKmH",
                table: "PhysicalResources",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CapacityPerHour",
                table: "PhysicalResources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalWindow",
                table: "PhysicalResources",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageSpeedKmH",
                table: "PhysicalResources");

            migrationBuilder.DropColumn(
                name: "CapacityPerHour",
                table: "PhysicalResources");

            migrationBuilder.DropColumn(
                name: "OperationalWindow",
                table: "PhysicalResources");
        }
    }
}
