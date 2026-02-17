using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class PopulatingPricingTiersAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PricingTierAvailabilities",
                columns: new[] { "Id", "IsAnnualAvailable", "IsMonthlyAvailable", "IsSemiAnnualAvailable" },
                values: new object[] { 1, true, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PricingTierAvailabilities",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
