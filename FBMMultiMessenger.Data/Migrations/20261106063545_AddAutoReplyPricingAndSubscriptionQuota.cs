using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoReplyPricingAndSubscriptionQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoReplyLimitUsed",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AutoReplyMaxLimit",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasAutoReply",
                table: "Subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AutoReplyMessageLimit",
                table: "PaymentVerifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AutoReplyPricingTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UptoMessages = table.Column<int>(type: "integer", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SemiAnnualPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoReplyPricingTiers", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AutoReplyLimitUsed", "AutoReplyMaxLimit", "HasAutoReply" },
                values: new object[] { 0, 0, false });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AutoReplyLimitUsed", "AutoReplyMaxLimit", "HasAutoReply" },
                values: new object[] { 0, 0, false });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AutoReplyLimitUsed", "AutoReplyMaxLimit", "HasAutoReply" },
                values: new object[] { 0, 0, false });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AutoReplyLimitUsed", "AutoReplyMaxLimit", "HasAutoReply" },
                values: new object[] { 0, 0, false });

            migrationBuilder.UpdateData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AutoReplyLimitUsed", "AutoReplyMaxLimit", "HasAutoReply" },
                values: new object[] { 0, 0, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoReplyPricingTiers");

            migrationBuilder.DropColumn(
                name: "AutoReplyLimitUsed",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "AutoReplyMaxLimit",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "HasAutoReply",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "AutoReplyMessageLimit",
                table: "PaymentVerifications");
        }
    }
}
