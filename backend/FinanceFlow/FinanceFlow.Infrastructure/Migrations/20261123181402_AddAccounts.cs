using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Tabela de contas
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    BankId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AccountLast4 = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    InitialBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InitialBalanceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "BRL"),
                    Color = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_BankId_AccountKeyHash",
                table: "Accounts",
                columns: new[] { "UserId", "BankId", "AccountKeyHash" },
                unique: true,
                filter: "[AccountKeyHash] IS NOT NULL AND [DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_Default",
                table: "Accounts",
                column: "UserId",
                unique: true,
                filter: "[IsDefault] = 1 AND [DeletedAt] IS NULL");

            // 2) Colunas AccountId nulas, só para poder fazer o backfill
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "BankImports",
                type: "uniqueidentifier",
                nullable: true);

            // 3) Backfill: uma "Conta principal" por usuário (inclusive os removidos,
            //    porque as transações deles continuam existindo)
            // Os comandos usam EXEC para rodar em lote próprio: ao gerar o script SQL (e rodá-lo
            // fora do EF), o SQL Server compila o lote inteiro antes de executar e não enxergaria
            // a coluna AccountId recém-criada pelo ALTER TABLE anterior.
            migrationBuilder.Sql(@"
EXEC(N'
INSERT INTO Accounts (Id, UserId, Name, Type, InitialBalance, Currency, Color, Icon, IsDefault, IsArchived, CreatedAt)
SELECT NEWID(), u.Id, N''Conta principal'', 1, 0,
       CASE WHEN LEN(u.Currency) = 3 THEN u.Currency ELSE ''BRL'' END,
       ''#10B981'', ''landmark'', 1, 0, SYSUTCDATETIME()
FROM Users u;')");

            migrationBuilder.Sql(@"
EXEC(N'
UPDATE t SET t.AccountId = a.Id
FROM Transactions t
INNER JOIN Accounts a ON a.UserId = t.UserId AND a.IsDefault = 1;')");

            migrationBuilder.Sql(@"
EXEC(N'
UPDATE b SET b.AccountId = a.Id
FROM BankImports b
INNER JOIN Accounts a ON a.UserId = b.UserId AND a.IsDefault = 1;')");

            // 4) Agora a coluna pode ser obrigatória
            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                table: "BankImports",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_Date",
                table: "Transactions",
                columns: new[] { "AccountId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_BankImports_AccountId",
                table: "BankImports",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankImports_Accounts_AccountId",
                table: "BankImports",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Accounts_AccountId",
                table: "Transactions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankImports_Accounts_AccountId",
                table: "BankImports");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Accounts_AccountId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_BankImports_AccountId",
                table: "BankImports");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "BankImports");
        }
    }
}
