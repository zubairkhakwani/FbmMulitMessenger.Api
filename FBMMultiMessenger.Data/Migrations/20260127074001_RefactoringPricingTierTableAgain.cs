using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactoringPricingTierTableAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SemiAnnualPricePerAccount",
                table: "PricingTiers",
                newName: "SemiAnnualPrice");

            migrationBuilder.RenameColumn(
                name: "MonthlyPricePerAccount",
                table: "PricingTiers",
                newName: "MonthlyPrice");

            migrationBuilder.RenameColumn(
                name: "AnnualPricePerAccount",
                table: "PricingTiers",
                newName: "AnnualPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SemiAnnualPrice",
                table: "PricingTiers",
                newName: "SemiAnnualPricePerAccount");

            migrationBuilder.RenameColumn(
                name: "MonthlyPrice",
                table: "PricingTiers",
                newName: "MonthlyPricePerAccount");

            migrationBuilder.RenameColumn(
                name: "AnnualPrice",
                table: "PricingTiers",
                newName: "AnnualPricePerAccount");
        }
    }
}
