using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSuccessionRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuccessionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    CandidateName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CurrentRole = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PotentialNextRole = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Readiness = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RiskOfLoss = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DevelopmentNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessionRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SuccessionRecords_ApplicationId",
                table: "SuccessionRecords",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuccessionRecords");
        }
    }
}
