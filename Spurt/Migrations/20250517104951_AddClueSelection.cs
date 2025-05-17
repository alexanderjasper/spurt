using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spurt.Migrations
{
    /// <inheritdoc />
    public partial class AddClueSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SelectedClueId",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnswered",
                table: "Clues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Games_SelectedClueId",
                table: "Games",
                column: "SelectedClueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Clues_SelectedClueId",
                table: "Games",
                column: "SelectedClueId",
                principalTable: "Clues",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Clues_SelectedClueId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_SelectedClueId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "SelectedClueId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsAnswered",
                table: "Clues");
        }
    }
}
