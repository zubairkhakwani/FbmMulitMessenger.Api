using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class addingLocalServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocalServerId",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocalServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TotalMemoryGB = table.Column<double>(type: "double precision", nullable: false),
                    LogicalProcessors = table.Column<int>(type: "integer", nullable: false),
                    ProcessorName = table.Column<string>(type: "text", nullable: false),
                    CoreCount = table.Column<int>(type: "integer", nullable: false),
                    MaxClockSpeedMHz = table.Column<int>(type: "integer", nullable: false),
                    CurrentClockSpeedMHz = table.Column<int>(type: "integer", nullable: false),
                    CpuArchitecture = table.Column<string>(type: "text", nullable: false),
                    CpuThreadCount = table.Column<int>(type: "integer", nullable: false),
                    GraphicsCards = table.Column<string>(type: "text", nullable: false),
                    TotalStorageGB = table.Column<double>(type: "double precision", nullable: false),
                    HasSSD = table.Column<bool>(type: "boolean", nullable: false),
                    OperatingSystem = table.Column<string>(type: "text", nullable: false),
                    Is64BitOS = table.Column<bool>(type: "boolean", nullable: false),
                    SystemUUID = table.Column<string>(type: "text", nullable: false),
                    MotherboardSerial = table.Column<string>(type: "text", nullable: false),
                    ProcessorId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalServers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LocalServerId",
                table: "Accounts",
                column: "LocalServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_LocalServers_LocalServerId",
                table: "Accounts",
                column: "LocalServerId",
                principalTable: "LocalServers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_LocalServers_LocalServerId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "LocalServers");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_LocalServerId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LocalServerId",
                table: "Accounts");
        }
    }
}
