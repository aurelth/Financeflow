using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TransferDirection",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransferMatchScore",
                table: "BankImportTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferMatchStatus",
                table: "BankImportTransactions",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferMatchTransactionId",
                table: "BankImportTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransferLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferLinks_Transactions_InTransactionId",
                        column: x => x.InTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferLinks_Transactions_OutTransactionId",
                        column: x => x.OutTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferLinks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferRejections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecondTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRejections", x => x.Id);
                });

            // Transferências importadas antes desta versão não guardavam o sentido. Ele é recuperado da linha
            // do extrato que as originou: crédito (Income) = entrada, débito (Expense) = saída.
            // Transferências sem linha de extrato, ou cujo tipo original se perdeu, ficam sem direção (não movem
            // o saldo e não entram no pareamento) até o usuário ajustá-las.
            // EXEC evita erro de compilação do batch (a coluna acabou de ser criada neste mesmo script).
            migrationBuilder.Sql(@"
EXEC(N'
UPDATE t
SET t.TransferDirection = CASE WHEN bit.Type = ''Income'' THEN 2 ELSE 1 END
FROM Transactions t
INNER JOIN BankImportTransactions bit ON bit.TransactionId = t.Id
WHERE t.Type = 3
  AND t.TransferDirection IS NULL
  AND bit.Type IN (''Income'', ''Expense'')
')");

            migrationBuilder.CreateIndex(
                name: "IX_TransferLinks_InTransactionId",
                table: "TransferLinks",
                column: "InTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferLinks_OutTransactionId",
                table: "TransferLinks",
                column: "OutTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferLinks_UserId",
                table: "TransferLinks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRejections_UserId_FirstTransactionId_SecondTransactionId",
                table: "TransferRejections",
                columns: new[] { "UserId", "FirstTransactionId", "SecondTransactionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferLinks");

            migrationBuilder.DropTable(
                name: "TransferRejections");

            migrationBuilder.DropColumn(
                name: "TransferDirection",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransferMatchScore",
                table: "BankImportTransactions");

            migrationBuilder.DropColumn(
                name: "TransferMatchStatus",
                table: "BankImportTransactions");

            migrationBuilder.DropColumn(
                name: "TransferMatchTransactionId",
                table: "BankImportTransactions");
        }
    }
}
