using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalApplicantFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "Jobs");
        }
    }
}
