using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddVvnAdditionalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaptainName",
                table: "VesselVisitNotifications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CrewCount",
                table: "VesselVisitNotifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadingManifestJson",
                table: "VesselVisitNotifications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PreferredDockCode",
                table: "VesselVisitNotifications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SafetyOfficersJson",
                table: "VesselVisitNotifications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UnloadingManifestJson",
                table: "VesselVisitNotifications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaptainName",
                table: "VesselVisitNotifications");

            migrationBuilder.DropColumn(
                name: "CrewCount",
                table: "VesselVisitNotifications");

            migrationBuilder.DropColumn(
                name: "LoadingManifestJson",
                table: "VesselVisitNotifications");

            migrationBuilder.DropColumn(
                name: "PreferredDockCode",
                table: "VesselVisitNotifications");

            migrationBuilder.DropColumn(
                name: "SafetyOfficersJson",
                table: "VesselVisitNotifications");

            migrationBuilder.DropColumn(
                name: "UnloadingManifestJson",
                table: "VesselVisitNotifications");
        }
    }
}
