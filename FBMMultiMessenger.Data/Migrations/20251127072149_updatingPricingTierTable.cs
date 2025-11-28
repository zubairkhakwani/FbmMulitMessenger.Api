using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatingPricingTierTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                column: "DiscountInPercentage",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 2,
                column: "DiscountInPercentage",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 3,
                column: "DiscountInPercentage",
                value: 0m);

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 4,
                column: "DiscountInPercentage",
                value: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountInPercentage",
                table: "PricingTiers");
        }
    }
}
