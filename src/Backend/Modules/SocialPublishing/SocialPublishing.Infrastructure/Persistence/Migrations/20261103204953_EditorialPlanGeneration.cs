using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EditorialPlanGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EditorialPlanGenerations",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RangeStart = table.Column<DateOnly>(type: "date", nullable: false),
                    RangeEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedPostIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailedSlotsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorialPlanGenerations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditorialPlanGenerations_WorkspaceId_RangeStart_RangeEnd_Seed",
                schema: "social",
                table: "EditorialPlanGenerations",
                columns: new[] { "WorkspaceId", "RangeStart", "RangeEnd", "Seed" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditorialPlanGenerations",
                schema: "social");
        }
    }
}
