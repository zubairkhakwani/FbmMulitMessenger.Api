using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingProxiesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Proxy_ProxyId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Proxy_Users_UserId",
                table: "Proxy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Proxy",
                table: "Proxy");

            migrationBuilder.RenameTable(
                name: "Proxy",
                newName: "Proxies");

            migrationBuilder.RenameIndex(
                name: "IX_Proxy_UserId",
                table: "Proxies",
                newName: "IX_Proxies_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Proxies",
                table: "Proxies",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Proxies_ProxyId",
                table: "Accounts",
                column: "ProxyId",
                principalTable: "Proxies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proxies_Users_UserId",
                table: "Proxies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Proxies_ProxyId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Proxies_Users_UserId",
                table: "Proxies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Proxies",
                table: "Proxies");

            migrationBuilder.RenameTable(
                name: "Proxies",
                newName: "Proxy");

            migrationBuilder.RenameIndex(
                name: "IX_Proxies_UserId",
                table: "Proxy",
                newName: "IX_Proxy_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Proxy",
                table: "Proxy",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Proxy_ProxyId",
                table: "Accounts",
                column: "ProxyId",
                principalTable: "Proxy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proxy_Users_UserId",
                table: "Proxy",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
