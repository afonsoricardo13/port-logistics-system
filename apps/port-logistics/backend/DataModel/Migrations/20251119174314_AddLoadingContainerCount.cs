using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddLoadingContainerCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoadingContainerCount",
                table: "VesselVisitNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoadingContainerCount",
                table: "VesselVisitNotifications");
        }
    }
}
