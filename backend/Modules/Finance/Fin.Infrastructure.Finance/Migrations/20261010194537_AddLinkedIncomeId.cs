using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fin.Infrastructure.Finance.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkedIncomeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "linked_income_id",
                table: "ledger_entries",
                type: "bigint unsigned",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_linked_income",
                table: "ledger_entries",
                column: "linked_income_id");

            // Data move: the legacy backend kept this link inside the details JSON.
            migrationBuilder.Sql(
                "UPDATE ledger_entries SET linked_income_id = CAST(JSON_UNQUOTE(JSON_EXTRACT(details, '$.linkedIncomeId')) AS UNSIGNED) " +
                "WHERE linked_income_id IS NULL AND JSON_EXTRACT(details, '$.linkedIncomeId') IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_linked_income",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "linked_income_id",
                table: "ledger_entries");
        }
    }
}
