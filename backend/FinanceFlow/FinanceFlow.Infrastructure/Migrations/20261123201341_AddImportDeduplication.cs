using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seguro: ImportHash guarda SHA-256 em hexadecimal (64 caracteres)
            migrationBuilder.AlterColumn<string>(
                name: "ImportHash",
                table: "Transactions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Transactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuplicateReason",
                table: "BankImportTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MatchedTransactionId",
                table: "BankImportTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountKeyHash",
                table: "BankImports",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountLast4",
                table: "BankImports",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankId",
                table: "BankImports",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Lançamentos já importados passam a carregar o FITID de origem, para a deduplicação por FITID
            // também valer para importações anteriores a esta versão.
            // EXEC evita erro de compilação do batch (a coluna acabou de ser criada neste mesmo script).
            migrationBuilder.Sql(@"
EXEC(N'
UPDATE t
SET t.ExternalId = bit.ExternalId
FROM Transactions t
INNER JOIN BankImportTransactions bit ON bit.TransactionId = t.Id
WHERE t.ExternalId IS NULL
  AND bit.ExternalId IS NOT NULL
  AND LEN(bit.ExternalId) > 0
')");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_ExternalId",
                table: "Transactions",
                columns: new[] { "AccountId", "ExternalId" },
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_ImportHash",
                table: "Transactions",
                columns: new[] { "AccountId", "ImportHash" },
                filter: "[ImportHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_ExternalId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_ImportHash",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "DuplicateReason",
                table: "BankImportTransactions");

            migrationBuilder.DropColumn(
                name: "MatchedTransactionId",
                table: "BankImportTransactions");

            migrationBuilder.DropColumn(
                name: "AccountKeyHash",
                table: "BankImports");

            migrationBuilder.DropColumn(
                name: "AccountLast4",
                table: "BankImports");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "BankImports");

            migrationBuilder.AlterColumn<string>(
                name: "ImportHash",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
