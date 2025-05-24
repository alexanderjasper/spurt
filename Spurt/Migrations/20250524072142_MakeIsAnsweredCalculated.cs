using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spurt.Migrations
{
    /// <inheritdoc />
    public partial class MakeIsAnsweredCalculated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAnswered",
                table: "Clues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAnswered",
                table: "Clues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
