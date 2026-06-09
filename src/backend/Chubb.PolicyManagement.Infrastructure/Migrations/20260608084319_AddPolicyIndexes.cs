using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chubb.PolicyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Policies_EffectiveDate",
                table: "Policies",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_ExpiryDate",
                table: "Policies",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_FlaggedForReview",
                table: "Policies",
                column: "FlaggedForReview");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_LineOfBusiness",
                table: "Policies",
                column: "LineOfBusiness");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_Region",
                table: "Policies",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_Status",
                table: "Policies",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Policies_EffectiveDate",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_ExpiryDate",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_FlaggedForReview",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_LineOfBusiness",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_Region",
                table: "Policies");

            migrationBuilder.DropIndex(
                name: "IX_Policies_Status",
                table: "Policies");
        }
    }
}
