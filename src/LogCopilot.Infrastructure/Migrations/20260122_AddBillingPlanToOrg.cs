using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogCopilot.Infrastructure.Migrations
{
    public partial class AddBillingPlanToOrg : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BillingPlanId",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionStartDate",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrial",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentMonthUploadCount",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUploadCountReset",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<long>(
                name: "CurrentStorageBytes",
                table: "Organizations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_BillingPlanId",
                table: "Organizations",
                column: "BillingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_BillingPlans_BillingPlanId",
                table: "Organizations",
                column: "BillingPlanId",
                principalTable: "BillingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

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
                name: "SubscriptionStartDate",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsTrial",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CurrentMonthUploadCount",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "LastUploadCountReset",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CurrentStorageBytes",
                table: "Organizations");
        }
    }
}
