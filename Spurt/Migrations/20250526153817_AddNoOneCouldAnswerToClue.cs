using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spurt.Migrations
{
    /// <inheritdoc />
    public partial class AddNoOneCouldAnswerToClue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoOneCouldAnswer",
                table: "Clues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoOneCouldAnswer",
                table: "Clues");
        }
    }
}
