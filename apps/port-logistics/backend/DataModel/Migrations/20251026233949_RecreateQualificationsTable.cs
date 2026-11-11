using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// US 2.2.13: Recreate Qualifications table with correct schema
    /// Drops the old table with incorrect columns (Code, Name) 
    /// and creates new table with correct columns (QualificationName, RequiredNumber)
    /// </summary>
    public partial class RecreateQualificationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old Qualifications table with wrong schema
            migrationBuilder.DropTable(
                name: "Qualifications");

            // Create the Qualifications table with correct schema
            migrationBuilder.CreateTable(
                name: "Qualifications",
                columns: table => new
                {
                    QualificationName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequiredNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifications", x => x.QualificationName);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new table
            migrationBuilder.DropTable(
                name: "Qualifications");

            // Recreate the old table with wrong schema (for rollback purposes)
            migrationBuilder.CreateTable(
                name: "Qualifications",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualifications", x => x.Code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
