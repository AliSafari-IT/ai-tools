using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogCopilot.Infrastructure.Migrations
{
    public partial class AddProviderTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedProvider",
                table: "IncidentReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualProvider",
                table: "IncidentReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderStatus",
                table: "IncidentReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderErrorSummary",
                table: "IncidentReports",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedProvider",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ActualProvider",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ProviderStatus",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ProviderErrorSummary",
                table: "IncidentReports");
        }
    }
}
