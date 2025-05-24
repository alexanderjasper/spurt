using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spurt.Migrations
{
    /// <inheritdoc />
    public partial class CalculatedScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Players");

            migrationBuilder.AddColumn<Guid>(
                name: "AnsweredByPlayerId",
                table: "Clues",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clues_AnsweredByPlayerId",
                table: "Clues",
                column: "AnsweredByPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clues_Players_AnsweredByPlayerId",
                table: "Clues",
                column: "AnsweredByPlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clues_Players_AnsweredByPlayerId",
                table: "Clues");

            migrationBuilder.DropIndex(
                name: "IX_Clues_AnsweredByPlayerId",
                table: "Clues");

            migrationBuilder.DropColumn(
                name: "AnsweredByPlayerId",
                table: "Clues");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
