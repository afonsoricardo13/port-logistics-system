using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddDataRightsRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataRightsRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FieldsToRectify = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewValues = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PreferredFormat = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ProcessedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseNotes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectionReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRightsRequests", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataRightsRequests");
        }
    }
}
