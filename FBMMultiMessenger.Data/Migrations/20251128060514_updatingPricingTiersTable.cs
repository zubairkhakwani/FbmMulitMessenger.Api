using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatingPricingTiersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "IsBaseValue",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PricingTiers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PricingTiers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsBaseValue",
                table: "PricingTiers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PricingTiers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "IsBaseValue", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "IsBaseValue", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "IsBaseValue", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "IsBaseValue", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 11, 17, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });
        }
    }
}
