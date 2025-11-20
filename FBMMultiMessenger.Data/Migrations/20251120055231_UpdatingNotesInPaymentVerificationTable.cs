using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBMMultiMessenger.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingNotesInPaymentVerificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Note",
                table: "PaymentVerifications",
                newName: "SubmissionNote");

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "PaymentVerifications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "PaymentVerifications");

            migrationBuilder.RenameColumn(
                name: "SubmissionNote",
                table: "PaymentVerifications",
                newName: "Note");
        }
    }
}
