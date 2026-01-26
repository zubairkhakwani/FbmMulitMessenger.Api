using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingPricingTierTableAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.RenameColumn(
                name: "SemiAnnualPrice",
                table: "PricingTiers",
                newName: "SemiAnnualPricePerAccount");

            migrationBuilder.RenameColumn(
                name: "PricePerAccount",
                table: "PricingTiers",
                newName: "MonthlyPricePerAccount");

            migrationBuilder.RenameColumn(
                name: "AnnualPrice",
                table: "PricingTiers",
                newName: "AnnualPricePerAccount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SemiAnnualPricePerAccount",
                table: "PricingTiers",
                newName: "SemiAnnualPrice");

            migrationBuilder.RenameColumn(
                name: "MonthlyPricePerAccount",
                table: "PricingTiers",
                newName: "PricePerAccount");

            migrationBuilder.RenameColumn(
                name: "AnnualPricePerAccount",
                table: "PricingTiers",
                newName: "AnnualPrice");

            migrationBuilder.InsertData(
                table: "PricingTiers",
                columns: new[] { "Id", "AnnualPrice", "MaxAccounts", "MinAccounts", "PricePerAccount", "SemiAnnualPrice" },
                values: new object[,]
                {
                    { 1, 0m, 10, 1, 100m, 0m },
                    { 2, 0m, 20, 11, 50m, 0m },
                    { 3, 0m, 100, 21, 40m, 0m },
                    { 4, 0m, 2147483647, 101, 30m, 0m }
                });
        }
    }
}
