using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstallmentGroupId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InstallmentGroupId_InstallmentNumber",
                table: "Transactions",
                columns: new[] { "InstallmentGroupId", "InstallmentNumber" },
                filter: "[InstallmentGroupId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_InstallmentGroupId_InstallmentNumber",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InstallmentGroupId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "Transactions");
        }
    }
}
