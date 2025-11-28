using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovingUnnecssaryProperiesInPricingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountInPKR",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "DiscountInPercentage",
                table: "PricingTiers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountInPKR",
                table: "PricingTiers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountInPercentage",
                table: "PricingTiers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DiscountInPKR", "DiscountInPercentage" },
                values: new object[] { 0m, 0m });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DiscountInPKR", "DiscountInPercentage" },
                values: new object[] { 0m, 0m });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DiscountInPKR", "DiscountInPercentage" },
                values: new object[] { 0m, 0m });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DiscountInPKR", "DiscountInPercentage" },
                values: new object[] { 0m, 0m });
        }
    }
}
