using System;
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
                name: "ApprovedByAdminId",
                table: "PaymentVerifications",
                newName: "HandledByUserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "PaymentVerifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionReason",
                table: "PaymentVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentVerifications_HandledByUserId",
                table: "PaymentVerifications",
                column: "HandledByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentVerifications_Users_HandledByUserId",
                table: "PaymentVerifications",
                column: "HandledByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentVerifications_Users_HandledByUserId",
                table: "PaymentVerifications");

            migrationBuilder.DropIndex(
                name: "IX_PaymentVerifications_HandledByUserId",
                table: "PaymentVerifications");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "PaymentVerifications");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "PaymentVerifications");

            migrationBuilder.RenameColumn(
                name: "HandledByUserId",
                table: "PaymentVerifications",
                newName: "ApprovedByAdminId");
        }
    }
}
