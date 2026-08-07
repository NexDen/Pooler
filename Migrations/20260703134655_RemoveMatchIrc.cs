using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolerBotCS.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMatchIrc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchIrc",
                table: "BanchoLobbies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MatchIrc",
                table: "BanchoLobbies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
