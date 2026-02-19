using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamingAccountLogoutReasonToReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogoutReason",
                table: "Accounts",
                newName: "Reason");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "Accounts",
                newName: "LogoutReason");
        }
    }
}
