using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingLocalServerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveBrowserCount",
                table: "LocalServers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxBrowserCapacity",
                table: "LocalServers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UniqueId",
                table: "LocalServers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveBrowserCount",
                table: "LocalServers");

            migrationBuilder.DropColumn(
                name: "MaxBrowserCapacity",
                table: "LocalServers");

            migrationBuilder.DropColumn(
                name: "UniqueId",
                table: "LocalServers");
        }
    }
}
