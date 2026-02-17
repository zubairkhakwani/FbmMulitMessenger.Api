using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingPricingTierAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnnualAvailable",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "IsMonthlyAvailable",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "IsSemiAnnualAvailable",
                table: "PricingTiers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnnualAvailable",
                table: "PricingTiers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMonthlyAvailable",
                table: "PricingTiers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSemiAnnualAvailable",
                table: "PricingTiers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
