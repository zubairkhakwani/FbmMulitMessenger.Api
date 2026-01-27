using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactoringPricingTierTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAccounts",
                table: "PricingTiers");

            migrationBuilder.RenameColumn(
                name: "MinAccounts",
                table: "PricingTiers",
                newName: "UptoAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UptoAccounts",
                table: "PricingTiers",
                newName: "MinAccounts");

            migrationBuilder.AddColumn<int>(
                name: "MaxAccounts",
                table: "PricingTiers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
