using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolerBotCS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanchoLobbies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MatchId = table.Column<string>(type: "text", nullable: false),
                    MatchIrc = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanchoLobbies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanchoPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    CurrentMatchId = table.Column<string>(type: "text", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    IsHost = table.Column<bool>(type: "boolean", nullable: false),
                    IsReady = table.Column<bool>(type: "boolean", nullable: false),
                    BanchoLobbyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanchoPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanchoPlayers_BanchoLobbies_BanchoLobbyId",
                        column: x => x.BanchoLobbyId,
                        principalTable: "BanchoLobbies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanchoPlayers_BanchoLobbyId",
                table: "BanchoPlayers",
                column: "BanchoLobbyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanchoPlayers");

            migrationBuilder.DropTable(
                name: "BanchoLobbies");
        }
    }
}
