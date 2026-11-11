using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmittedByRepresentativeIdToVVN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubmittedByRepresentativeId",
                table: "VesselVisitNotifications",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubmittedByRepresentativeId",
                table: "VesselVisitNotifications");
        }
    }
}
