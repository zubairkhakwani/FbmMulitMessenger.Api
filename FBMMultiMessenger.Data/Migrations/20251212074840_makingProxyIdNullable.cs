using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class makingProxyIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Proxies_ProxyId",
                table: "Accounts");

            migrationBuilder.AlterColumn<int>(
                name: "ProxyId",
                table: "Accounts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Proxies_ProxyId",
                table: "Accounts",
                column: "ProxyId",
                principalTable: "Proxies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Proxies_ProxyId",
                table: "Accounts");

            migrationBuilder.AlterColumn<int>(
                name: "ProxyId",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Proxies_ProxyId",
                table: "Accounts",
                column: "ProxyId",
                principalTable: "Proxies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
