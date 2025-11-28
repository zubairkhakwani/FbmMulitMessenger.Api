using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingPropertiesInPricintTierTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountInPKR",
                table: "PricingTiers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsBaseValue",
                table: "PricingTiers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DiscountInPKR", "IsBaseValue" },
                values: new object[] { 0m, false });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DiscountInPKR", "IsBaseValue" },
                values: new object[] { 0m, false });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DiscountInPKR", "IsBaseValue" },
                values: new object[] { 0m, false });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DiscountInPKR", "IsBaseValue" },
                values: new object[] { 0m, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountInPKR",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "IsBaseValue",
                table: "PricingTiers");
        }
    }
}
