using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LocalServers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LocalServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "LocalServers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LocalServers_UserId",
                table: "LocalServers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LocalServers_Users_UserId",
                table: "LocalServers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocalServers_Users_UserId",
                table: "LocalServers");

            migrationBuilder.DropIndex(
                name: "IX_LocalServers_UserId",
                table: "LocalServers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LocalServers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LocalServers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LocalServers");
        }
    }
}
