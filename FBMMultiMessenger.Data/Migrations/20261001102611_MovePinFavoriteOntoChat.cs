using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovePinFavoriteOntoChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxFavoriteChats",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "MaxPinnedChats",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "FavoriteOrder",
                table: "Chats",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Chats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Chats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PinOrder",
                table: "Chats",
                type: "integer",
                nullable: true);

            // Copy legacy prefs into Chat columns before dropping the prefs table.
            migrationBuilder.Sql("""
                UPDATE "Chats" AS c
                SET
                    "IsPinned" = p."IsPinned",
                    "IsFavorite" = p."IsFavorite",
                    "PinOrder" = p."PinOrder",
                    "FavoriteOrder" = p."FavoriteOrder"
                FROM "UserChatListPreferences" AS p
                WHERE c."Id" = p."ChatId" AND c."UserId" = p."UserId";
                """);

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "MaxPinnedChats" = 5
                WHERE "MaxPinnedChats" = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "MaxFavoriteChats" = 50
                WHERE "MaxFavoriteChats" = 0;
                """);

            migrationBuilder.DropTable(
                name: "UserChatListPreferences");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MaxFavoriteChats", "MaxPinnedChats" },
                values: new object[] { 50, 5 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "MaxFavoriteChats", "MaxPinnedChats" },
                values: new object[] { 50, 5 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "MaxFavoriteChats", "MaxPinnedChats" },
                values: new object[] { 50, 5 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "MaxFavoriteChats", "MaxPinnedChats" },
                values: new object[] { 50, 5 });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "MaxFavoriteChats", "MaxPinnedChats" },
                values: new object[] { 50, 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserChatListPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FavoriteOrder = table.Column<int>(type: "integer", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    PinOrder = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChatListPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserChatListPreferences_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserChatListPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO "UserChatListPreferences"
                    ("UserId", "ChatId", "IsPinned", "IsFavorite", "PinOrder", "FavoriteOrder", "CreatedAt", "UpdatedAt")
                SELECT
                    c."UserId",
                    c."Id",
                    c."IsPinned",
                    c."IsFavorite",
                    c."PinOrder",
                    c."FavoriteOrder",
                    NOW(),
                    NOW()
                FROM "Chats" AS c
                WHERE c."IsPinned" = TRUE OR c."IsFavorite" = TRUE;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserChatListPreferences_ChatId",
                table: "UserChatListPreferences",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_UserChatListPreferences_UserId_ChatId",
                table: "UserChatListPreferences",
                columns: new[] { "UserId", "ChatId" },
                unique: true);

            migrationBuilder.DropColumn(
                name: "MaxFavoriteChats",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxPinnedChats",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FavoriteOrder",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "PinOrder",
                table: "Chats");
        }
    }
}
