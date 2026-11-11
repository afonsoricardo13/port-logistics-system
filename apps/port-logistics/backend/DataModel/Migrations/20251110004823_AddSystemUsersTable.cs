using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemUsers",
                columns: table => new
                {
                    Email = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActivationToken = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActivationTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUsers", x => x.Email);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsers_ActivationToken",
                table: "SystemUsers",
                column: "ActivationToken",
                unique: true,
                filter: "ActivationToken IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SystemUsers_Email",
                table: "SystemUsers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemUsers");
        }
    }
}
