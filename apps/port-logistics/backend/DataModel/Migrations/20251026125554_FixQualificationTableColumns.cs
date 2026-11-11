using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// US 2.2.13: Add Qualifications table with correct schema
    /// - QualificationName (string, PK) - unique identifier
    /// - RequiredNumber (int) - required staff count
    /// This migration fixes the initial incorrect schema (Code, Name)
    /// </summary>
    public partial class AddQualificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename Code column to QualificationName
            migrationBuilder.RenameColumn(
                name: "Code",
                table: "Qualifications",
                newName: "QualificationName");

            // Rename Name column to RequiredNumber (this won't work directly, need to add and copy)
            // First, add the new RequiredNumber column
            migrationBuilder.AddColumn<int>(
                name: "RequiredNumber",
                table: "Qualifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Copy data from Name to RequiredNumber (assuming Name contains numbers)
            // This SQL assumes Name column contains numeric values or can be converted
            migrationBuilder.Sql("UPDATE `Qualifications` SET `RequiredNumber` = 0;");

            // Drop the old Name column
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Qualifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes
            // Add back Name column
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Qualifications",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // Drop RequiredNumber column
            migrationBuilder.DropColumn(
                name: "RequiredNumber",
                table: "Qualifications");

            // Rename QualificationName back to Code
            migrationBuilder.RenameColumn(
                name: "QualificationName",
                table: "Qualifications",
                newName: "Code");
        }
    }
}
