using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovingIsExpiredFromSubscriptionAndAddingIsActiveBit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsExpired",
                table: "Subscriptions",
                newName: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Subscriptions",
                newName: "IsExpired");
        }
    }
}
