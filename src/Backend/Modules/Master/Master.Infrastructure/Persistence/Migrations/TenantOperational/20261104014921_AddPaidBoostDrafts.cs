using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational
{
    /// <inheritdoc />
    public partial class AddPaidBoostDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaidBoostDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganicPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedAdAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MediaObjectKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalAdId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaidBoostDrafts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaidBoostDrafts_WorkspaceId_SuggestionId",
                table: "PaidBoostDrafts",
                columns: new[] { "WorkspaceId", "SuggestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaidBoostDrafts");
        }
    }
}
