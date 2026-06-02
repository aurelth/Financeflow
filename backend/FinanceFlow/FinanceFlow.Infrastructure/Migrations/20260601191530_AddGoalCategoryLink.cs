using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalCategoryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedCategoryId",
                table: "Goals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoalCategory",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Goals_LinkedCategoryId",
                table: "Goals",
                column: "LinkedCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Categories_LinkedCategoryId",
                table: "Goals",
                column: "LinkedCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Categories_LinkedCategoryId",
                table: "Goals");

            migrationBuilder.DropIndex(
                name: "IX_Goals_LinkedCategoryId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "LinkedCategoryId",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsGoalCategory",
                table: "Categories");
        }
    }
}
