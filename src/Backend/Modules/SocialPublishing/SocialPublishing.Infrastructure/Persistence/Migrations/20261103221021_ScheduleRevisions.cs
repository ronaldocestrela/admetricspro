using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleRevisions",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganicPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleRevisions_OrganicPosts_OrganicPostId",
                        column: x => x.OrganicPostId,
                        principalSchema: "social",
                        principalTable: "OrganicPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleRevisions_OrganicPostId",
                schema: "social",
                table: "ScheduleRevisions",
                column: "OrganicPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleRevisions",
                schema: "social");
        }
    }
}
