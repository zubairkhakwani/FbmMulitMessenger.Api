using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingPaymentVerificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountsPurchased",
                table: "PaymentVerifications",
                newName: "BillingCylce");

            migrationBuilder.AddColumn<int>(
                name: "AccountLimit",
                table: "PaymentVerifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "BasePricePerMonth",
                table: "PaymentVerifications",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SavingAmount",
                table: "PaymentVerifications",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountLimit",
                table: "PaymentVerifications");

            migrationBuilder.DropColumn(
                name: "BasePricePerMonth",
                table: "PaymentVerifications");

            migrationBuilder.DropColumn(
                name: "SavingAmount",
                table: "PaymentVerifications");

            migrationBuilder.RenameColumn(
                name: "BillingCylce",
                table: "PaymentVerifications",
                newName: "AccountsPurchased");
        }
    }
}
