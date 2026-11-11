using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "PhysicalResources",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "PhysicalResources",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StorageAreas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Identifier = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxCapacityTeu = table.Column<int>(type: "int", nullable: false),
                    CurrentOccupancyTeu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageAreas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StorageAreaServedDocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StorageAreaId = table.Column<long>(type: "bigint", nullable: false),
                    DockCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DistanceMeters = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageAreaServedDocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StorageAreaServedDocks_StorageAreas_StorageAreaId",
                        column: x => x.StorageAreaId,
                        principalTable: "StorageAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_StorageAreaServedDocks_StorageAreaId",
                table: "StorageAreaServedDocks",
                column: "StorageAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_StorageAreas_Identifier",
                table: "StorageAreas",
                column: "Identifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StorageAreaServedDocks");

            migrationBuilder.DropTable(
                name: "StorageAreas");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "PhysicalResources",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "PhysicalResources",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
