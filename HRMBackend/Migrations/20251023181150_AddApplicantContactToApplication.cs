using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRMBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantContactToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ResumeUrl",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CandidateUserId",
                table: "Applications",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ApplicantEmail",
                table: "Applications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantFullName",
                table: "Applications",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantPhone",
                table: "Applications",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicantEmail",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApplicantFullName",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApplicantPhone",
                table: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "ResumeUrl",
                table: "Applications",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CandidateUserId",
                table: "Applications",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
