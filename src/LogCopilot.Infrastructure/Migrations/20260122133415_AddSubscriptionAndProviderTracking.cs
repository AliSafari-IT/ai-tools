using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogCopilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAndProviderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillingPlanId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentMonthUploadCount",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "CurrentStorageBytes",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrial",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUploadCountReset",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActualProvider",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderErrorSummary",
                table: "IncidentReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderStatus",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedProvider",
                table: "IncidentReports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnablesAiReports",
                table: "BillingPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnablesSemanticClustering",
                table: "BillingPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "ApiKeys",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_BillingPlanId",
                table: "Organizations",
                column: "BillingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_BillingPlans_BillingPlanId",
                table: "Organizations",
                column: "BillingPlanId",
                principalTable: "BillingPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_BillingPlans_BillingPlanId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_BillingPlanId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "BillingPlanId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CurrentMonthUploadCount",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CurrentStorageBytes",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsTrial",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastUploadCountReset",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionStartDate",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ActualProvider",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ProviderErrorSummary",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "ProviderStatus",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "RequestedProvider",
                table: "IncidentReports");

            migrationBuilder.DropColumn(
                name: "EnablesAiReports",
                table: "BillingPlans");

            migrationBuilder.DropColumn(
                name: "EnablesSemanticClustering",
                table: "BillingPlans");

            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "ApiKeys");
        }
    }
}
