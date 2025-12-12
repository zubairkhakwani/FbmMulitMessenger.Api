using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingProxyUserWise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Proxy",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Proxy_UserId",
                table: "Proxy",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proxy_Users_UserId",
                table: "Proxy",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proxy_Users_UserId",
                table: "Proxy");

            migrationBuilder.DropIndex(
                name: "IX_Proxy_UserId",
                table: "Proxy");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Proxy");
        }
    }
}
