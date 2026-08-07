using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolerBotCS.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentMapAndFixBanchoPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentMatchId",
                table: "BanchoPlayers");

            migrationBuilder.AddColumn<string>(
                name: "CurrentMapId",
                table: "BanchoLobbies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentMapId",
                table: "BanchoLobbies");

            migrationBuilder.AddColumn<string>(
                name: "CurrentMatchId",
                table: "BanchoPlayers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
