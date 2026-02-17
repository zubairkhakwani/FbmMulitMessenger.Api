using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingPropertiesInPricingTiersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAvailable",
                table: "PricingTiers",
                newName: "IsSemiAnnualAvailable");

            migrationBuilder.AddColumn<bool>(
                name: "HasAvailedTrial",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "HasAvailedTrial",
                value: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "HasAvailedTrial",
                value: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "HasAvailedTrial",
                value: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                column: "HasAvailedTrial",
                value: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                column: "HasAvailedTrial",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAvailedTrial",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAnnualAvailable",
                table: "PricingTiers");

            migrationBuilder.DropColumn(
                name: "IsMonthlyAvailable",
                table: "PricingTiers");

            migrationBuilder.RenameColumn(
                name: "IsSemiAnnualAvailable",
                table: "PricingTiers",
                newName: "IsAvailable");
        }
    }
}
