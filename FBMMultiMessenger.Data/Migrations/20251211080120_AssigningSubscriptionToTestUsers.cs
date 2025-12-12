using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class AssigningSubscriptionToTestUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "Id", "CanRunOnOurServer", "ExpiredAt", "IsActive", "LimitUsed", "MaxLimit", "StartedAt", "UserId" },
                values: new object[,]
                {
                    { 3, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 50, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 3 },
                    { 4, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 50, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 4 },
                    { 5, false, new DateTime(2025, 12, 31, 0, 0, 0, 0, DateTimeKind.Utc), false, 0, 1000, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), 5 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
