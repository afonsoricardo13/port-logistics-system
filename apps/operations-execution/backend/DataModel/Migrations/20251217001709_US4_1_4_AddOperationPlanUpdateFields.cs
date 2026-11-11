using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class US4_1_4_AddOperationPlanUpdateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChangeLogs",
                table: "OperationPlans",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OperationPlans",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "OperationPlans",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OperationPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangeLogs",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "OperationPlans");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OperationPlans");
        }
    }
}
