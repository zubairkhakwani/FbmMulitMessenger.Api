using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionLastUpdatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastUpdatedByUserId",
                table: "Subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastUpdatedAt", "LastUpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastUpdatedAt", "LastUpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "LastUpdatedAt", "LastUpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "LastUpdatedAt", "LastUpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "LastUpdatedAt", "LastUpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_LastUpdatedByUserId",
                table: "Subscriptions",
                column: "LastUpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Users_LastUpdatedByUserId",
                table: "Subscriptions",
                column: "LastUpdatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Users_LastUpdatedByUserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_LastUpdatedByUserId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "LastUpdatedByUserId",
                table: "Subscriptions");
        }
    }
}
