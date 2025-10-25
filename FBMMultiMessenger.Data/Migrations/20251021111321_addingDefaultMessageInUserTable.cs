using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingDefaultMessageInUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DefaultMessage_DefaultMessageId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_DefaultMessage_Users_UserId",
                table: "DefaultMessage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DefaultMessage",
                table: "DefaultMessage");

            migrationBuilder.RenameTable(
                name: "DefaultMessage",
                newName: "DefaultMessages");

            migrationBuilder.RenameIndex(
                name: "IX_DefaultMessage_UserId",
                table: "DefaultMessages",
                newName: "IX_DefaultMessages_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DefaultMessages",
                table: "DefaultMessages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DefaultMessages_DefaultMessageId",
                table: "Accounts",
                column: "DefaultMessageId",
                principalTable: "DefaultMessages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DefaultMessages_Users_UserId",
                table: "DefaultMessages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DefaultMessages_DefaultMessageId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_DefaultMessages_Users_UserId",
                table: "DefaultMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DefaultMessages",
                table: "DefaultMessages");

            migrationBuilder.RenameTable(
                name: "DefaultMessages",
                newName: "DefaultMessage");

            migrationBuilder.RenameIndex(
                name: "IX_DefaultMessages_UserId",
                table: "DefaultMessage",
                newName: "IX_DefaultMessage_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DefaultMessage",
                table: "DefaultMessage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DefaultMessage_DefaultMessageId",
                table: "Accounts",
                column: "DefaultMessageId",
                principalTable: "DefaultMessage",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DefaultMessage_Users_UserId",
                table: "DefaultMessage",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
