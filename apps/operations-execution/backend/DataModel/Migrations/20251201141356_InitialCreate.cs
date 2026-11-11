using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ComplementaryTaskCategories",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultDuration = table.Column<int>(type: "int", nullable: true),
                    ExpectedImpact = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplementaryTaskCategories", x => x.Code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IncidentTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    ParentTypeCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTypes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_IncidentTypes_IncidentTypes_ParentTypeCode",
                        column: x => x.ParentTypeCode,
                        principalTable: "IncidentTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OperationPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VvnId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Operations = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedResources = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TimeWindows = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AlgorithmUsed = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metadata = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationPlans", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VesselVisitExecutions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VvnId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualArrivalTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActualBerthTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DockUsed = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExecutedOperations = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualUnberthTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActualDepartureTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VesselVisitExecutions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IncidentTypeCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AffectedVVEs = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_IncidentTypes_IncidentTypeCode",
                        column: x => x.IncidentTypeCode,
                        principalTable: "IncidentTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ComplementaryTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CategoryCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VveId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponsibleTeam = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ImpactType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplementaryTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplementaryTasks_ComplementaryTaskCategories_CategoryCode",
                        column: x => x.CategoryCode,
                        principalTable: "ComplementaryTaskCategories",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplementaryTasks_VesselVisitExecutions_VveId",
                        column: x => x.VveId,
                        principalTable: "VesselVisitExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ComplementaryTasks_CategoryCode",
                table: "ComplementaryTasks",
                column: "CategoryCode");

            migrationBuilder.CreateIndex(
                name: "IX_ComplementaryTasks_Status",
                table: "ComplementaryTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ComplementaryTasks_VveId",
                table: "ComplementaryTasks",
                column: "VveId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTypes_ParentTypeCode",
                table: "IncidentTypes",
                column: "ParentTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentTypeCode",
                table: "Incidents",
                column: "IncidentTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_StartTime",
                table: "Incidents",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_Status",
                table: "Incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_Date",
                table: "OperationPlans",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_OperationPlans_VvnId",
                table: "OperationPlans",
                column: "VvnId");

            migrationBuilder.CreateIndex(
                name: "IX_VesselVisitExecutions_Status",
                table: "VesselVisitExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VesselVisitExecutions_VvnId",
                table: "VesselVisitExecutions",
                column: "VvnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplementaryTasks");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "OperationPlans");

            migrationBuilder.DropTable(
                name: "ComplementaryTaskCategories");

            migrationBuilder.DropTable(
                name: "VesselVisitExecutions");

            migrationBuilder.DropTable(
                name: "IncidentTypes");
        }
    }
}
