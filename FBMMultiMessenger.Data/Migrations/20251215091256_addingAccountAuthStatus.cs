using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingAccountAuthStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Accounts",
                newName: "ConnectionStatus");

            migrationBuilder.AddColumn<int>(
                name: "AccountLoginStatus",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountLoginStatus",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "ConnectionStatus",
                table: "Accounts",
                newName: "Status");
        }
    }
}
