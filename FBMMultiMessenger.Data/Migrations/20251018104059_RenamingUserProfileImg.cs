using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamingUserProfileImg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserProfileImg",
                table: "Chats",
                newName: "UserProfileImage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserProfileImage",
                table: "Chats",
                newName: "UserProfileImg");
        }
    }
}
