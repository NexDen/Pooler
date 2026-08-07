using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolerBotCS.Migrations
{
    /// <inheritdoc />
    public partial class RemovedPlayerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHost",
                table: "BanchoPlayers");

            migrationBuilder.DropColumn(
                name: "IsReady",
                table: "BanchoPlayers");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "BanchoPlayers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHost",
                table: "BanchoPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReady",
                table: "BanchoPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "BanchoPlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
