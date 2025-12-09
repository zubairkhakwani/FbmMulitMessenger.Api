using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingNewRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanRunOnOurServer",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Name" },
                values: new object[] { 4, new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "SuperServer" });


            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CanRunOnOurServer",
                value: false);

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CanRunOnOurServer",
                value: false);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ContactNumber", "CreatedAt", "Email", "IsActive", "IsEmailVerified", "Name", "Password", "RoleId" },
                values: new object[] { 5, "03330337272", new DateTime(2025, 9, 20, 0, 0, 0, 0, DateTimeKind.Utc), "super@gmail.com", true, false, "Super_Server", "Super1!", 4 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "CanRunOnOurServer",
                table: "Subscriptions");
        }
    }
}
