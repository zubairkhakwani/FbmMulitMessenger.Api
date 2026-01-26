using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingPricingTierTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualPrice",
                table: "PricingTiers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SemiAnnualPrice",
                table: "PricingTiers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AnnualPrice", "SemiAnnualPrice" },
                values: new object[] { 0m, 0m });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AnnualPrice", "SemiAnnualPrice" },
                values: new object[] { 0m, 0m });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AnnualPrice", "SemiAnnualPrice" },
                values: new object[] { 0m, 0m });

            migrationBuilder.UpdateData(
                table: "PricingTiers",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AnnualPrice", "SemiAnnualPrice" },
                values: new object[] { 0m, 0m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualPrice",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "SemiAnnualPrice",
                table: "PricingTiers");
        }
    }
}
