using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolerBotCS.Migrations
{
    /// <inheritdoc />
    public partial class AddPoolerMapEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInLobby",
                table: "BanchoPlayers",
                type: "boolean",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "CurrentMapId",
                table: "BanchoLobbies");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentMapId",
                table: "BanchoLobbies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PoolerMaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BeatmapId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Mods = table.Column<int>(type: "integer", nullable: false),
                    BanchoLobbyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolerMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoolerMaps_BanchoLobbies_BanchoLobbyId",
                        column: x => x.BanchoLobbyId,
                        principalTable: "BanchoLobbies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanchoLobbies_CurrentMapId",
                table: "BanchoLobbies",
                column: "CurrentMapId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolerMaps_BanchoLobbyId",
                table: "PoolerMaps",
                column: "BanchoLobbyId");

            migrationBuilder.AddForeignKey(
                name: "FK_BanchoLobbies_PoolerMaps_CurrentMapId",
                table: "BanchoLobbies",
                column: "CurrentMapId",
                principalTable: "PoolerMaps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanchoLobbies_PoolerMaps_CurrentMapId",
                table: "BanchoLobbies");

            migrationBuilder.DropTable(
                name: "PoolerMaps");

            migrationBuilder.DropIndex(
                name: "IX_BanchoLobbies_CurrentMapId",
                table: "BanchoLobbies");

            migrationBuilder.DropColumn(
                name: "IsInLobby",
                table: "BanchoPlayers");

            migrationBuilder.DropColumn(
                name: "CurrentMapId",
                table: "BanchoLobbies");

            migrationBuilder.AddColumn<string>(
                name: "CurrentMapId",
                table: "BanchoLobbies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
