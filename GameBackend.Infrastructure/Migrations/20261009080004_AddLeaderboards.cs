using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GameBackend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScoreEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaderboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<long>(type: "bigint", nullable: false),
                    AchievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreEntries_Leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalTable: "Leaderboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoreEntries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Leaderboards",
                columns: new[] { "Id", "Key", "Name" },
                values: new object[,]
                {
                    { new Guid("0b6f2a3e-6c1f-4f4e-9a51-6d2a1f3c9e01"), "classic", "Classic" },
                    { new Guid("0b6f2a3e-6c1f-4f4e-9a51-6d2a1f3c9e02"), "endless", "Endless" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_Key",
                table: "Leaderboards",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreEntries_LeaderboardId_PlayerId",
                table: "ScoreEntries",
                columns: new[] { "LeaderboardId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreEntries_LeaderboardId_Score",
                table: "ScoreEntries",
                columns: new[] { "LeaderboardId", "Score" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreEntries_PlayerId",
                table: "ScoreEntries",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoreEntries");

            migrationBuilder.DropTable(
                name: "Leaderboards");
        }
    }
}
