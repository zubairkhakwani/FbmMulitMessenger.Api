using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingColumnsToChatAndChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OtherUserId",
                table: "Chats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherUserName",
                table: "Chats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FBTimestamp",
                table: "ChatMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FbMessageId",
                table: "ChatMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtherUserId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "OtherUserName",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "FBTimestamp",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "FbMessageId",
                table: "ChatMessages");
        }
    }
}
