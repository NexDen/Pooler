using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PoolerBotCS.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Code = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanchoLobbies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IrcMatchId = table.Column<string>(type: "text", nullable: false),
                    AssignedMatchId = table.Column<string>(type: "text", nullable: false),
                    CurrentMapId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsReady = table.Column<bool>(type: "boolean", nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false),
                    LobbyStatus = table.Column<int>(type: "integer", nullable: false),
                    LobbyDiscordGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    LobbyDiscordChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
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
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveVerificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    CurrentRole = table.Column<int>(type: "integer", nullable: true),
                    IsDead = table.Column<bool>(type: "boolean", nullable: true),
                    IsInLobby = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_BanchoPlayers_Verifications_ActiveVerificationId",
                        column: x => x.ActiveVerificationId,
                        principalTable: "Verifications",
                        principalColumn: "Id");
                });

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
                name: "IX_BanchoPlayers_ActiveVerificationId",
                table: "BanchoPlayers",
                column: "ActiveVerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_BanchoPlayers_BanchoLobbyId",
                table: "BanchoPlayers",
                column: "BanchoLobbyId");

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
                name: "BanchoPlayers");

            migrationBuilder.DropTable(
                name: "Verifications");

            migrationBuilder.DropTable(
                name: "PoolerMaps");

            migrationBuilder.DropTable(
                name: "BanchoLobbies");
        }
    }
}
