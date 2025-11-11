using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalApplicantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "Applications",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "Applications");
        }
    }
}
