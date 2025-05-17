using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spurt.Migrations
{
    /// <inheritdoc />
    public partial class AddBuzzerFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BuzzedPlayerId",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BuzzedTime",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_BuzzedPlayerId",
                table: "Games",
                column: "BuzzedPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Players_BuzzedPlayerId",
                table: "Games",
                column: "BuzzedPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Players_BuzzedPlayerId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_BuzzedPlayerId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BuzzedPlayerId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "BuzzedTime",
                table: "Games");
        }
    }
}
