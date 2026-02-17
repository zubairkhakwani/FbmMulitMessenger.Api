using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingTrialRelatedPropertiesInSubscriptionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTrial",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialExpiresAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialStartedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsTrial", "TrialExpiresAt", "TrialStartedAt" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsTrial", "TrialExpiresAt", "TrialStartedAt" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsTrial", "TrialExpiresAt", "TrialStartedAt" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsTrial", "TrialExpiresAt", "TrialStartedAt" },
                values: new object[] { false, null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsTrial", "TrialExpiresAt", "TrialStartedAt" },
                values: new object[] { false, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTrial",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TrialExpiresAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "TrialStartedAt",
                table: "Subscriptions");
        }
    }
}
