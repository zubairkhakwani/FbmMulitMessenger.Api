using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class FbMessageIdSetToUniqueInChatMessageTableUPdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_FbMessageId",
                table: "ChatMessages");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_FbMessageId_ChatId",
                table: "ChatMessages",
                columns: new[] { "FbMessageId", "ChatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_FbMessageId_ChatId",
                table: "ChatMessages");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_FbMessageId",
                table: "ChatMessages",
                column: "FbMessageId",
                unique: true);
        }
    }
}
