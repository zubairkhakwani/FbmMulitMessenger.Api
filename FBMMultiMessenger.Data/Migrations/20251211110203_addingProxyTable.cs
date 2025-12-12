using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingProxyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "LocalServers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProxyId",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Proxy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ip_Port = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    IsActives = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proxy", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ProxyId",
                table: "Accounts",
                column: "ProxyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Proxy_ProxyId",
                table: "Accounts",
                column: "ProxyId",
                principalTable: "Proxy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_Proxy_ProxyId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "Proxy");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_ProxyId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "LocalServers");

            migrationBuilder.DropColumn(
                name: "ProxyId",
                table: "Accounts");
        }
    }
}
