using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportSummaryAndProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Provider",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "IncidentReports");
        }
    }
}
