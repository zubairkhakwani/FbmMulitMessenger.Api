using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIndexInChatAndAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_FBChatId_UserId",
                table: "Chats");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_FBChatId_UserId",
                table: "Chats",
                columns: new[] { "FBChatId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_FbAccountId_UserId",
                table: "Accounts",
                columns: new[] { "FbAccountId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_FBChatId_UserId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_FbAccountId_UserId",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_FBChatId_UserId",
                table: "Chats",
                columns: new[] { "FBChatId", "UserId" });
        }
    }
}
