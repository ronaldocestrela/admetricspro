using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PublicationJobsOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicationJobs",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_IdempotencyKey",
                schema: "social",
                table: "PublicationJobs",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_PostId_Status",
                schema: "social",
                table: "PublicationJobs",
                columns: new[] { "PostId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicationJobs_WorkspaceId_ScheduledAtUtc",
                schema: "social",
                table: "PublicationJobs",
                columns: new[] { "WorkspaceId", "ScheduledAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicationJobs",
                schema: "social");
        }
    }
}
