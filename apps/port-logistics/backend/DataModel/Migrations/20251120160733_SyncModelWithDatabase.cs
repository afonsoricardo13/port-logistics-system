using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalUnloadingContainers",
                table: "VesselVisitNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalUnloadingContainers",
                table: "VesselVisitNotifications");
        }
    }
}
