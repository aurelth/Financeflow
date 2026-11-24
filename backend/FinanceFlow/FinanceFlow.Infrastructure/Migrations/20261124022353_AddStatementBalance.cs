using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "BankImports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LedgerBalance",
                table: "BankImports",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LedgerBalanceDate",
                table: "BankImports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatementEnd",
                table: "BankImports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatementStart",
                table: "BankImports",
                type: "datetime2",
                nullable: true);

            // Importações antigas que já tinham lançamentos confirmados continuam contando como confirmadas.
            // EXEC porque o SQL Server valida o nome das colunas novas antes de a migration aplicá-las.
            migrationBuilder.Sql(
                "EXEC(N'UPDATE BankImports SET ConfirmedAt = CreatedAt WHERE Imported > 0 AND ConfirmedAt IS NULL')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "BankImports");

            migrationBuilder.DropColumn(
                name: "LedgerBalance",
                table: "BankImports");

            migrationBuilder.DropColumn(
                name: "LedgerBalanceDate",
                table: "BankImports");

            migrationBuilder.DropColumn(
                name: "StatementEnd",
                table: "BankImports");

            migrationBuilder.DropColumn(
                name: "StatementStart",
                table: "BankImports");
        }
    }
}
