using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial_SocialPublishingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "social");

            migrationBuilder.CreateTable(
                name: "BrandVoiceProfiles",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tone = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Persona = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmojiPolicy = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HashtagLimit = table.Column<int>(type: "int", nullable: false),
                    PillarAuthority = table.Column<int>(type: "int", nullable: false),
                    PillarSale = table.Column<int>(type: "int", nullable: false),
                    PillarConnection = table.Column<int>(type: "int", nullable: false),
                    PillarSocialProof = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ForbiddenTermsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MandatoryLinksJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandVoiceProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganicConnections",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExternalAccountId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EncryptedAccessToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AccessTokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganicPosts",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Pillar = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(2200)", maxLength: 2200, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicPosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganicMediaAssets",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    OrganicPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicMediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganicMediaAssets_OrganicPosts_OrganicPostId",
                        column: x => x.OrganicPostId,
                        principalSchema: "social",
                        principalTable: "OrganicPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandVoiceProfiles_WorkspaceId",
                schema: "social",
                table: "BrandVoiceProfiles",
                column: "WorkspaceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganicConnections_WorkspaceId",
                schema: "social",
                table: "OrganicConnections",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicConnections_WorkspaceId_Platform_ExternalAccountId",
                schema: "social",
                table: "OrganicConnections",
                columns: new[] { "WorkspaceId", "Platform", "ExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganicMediaAssets_OrganicPostId",
                schema: "social",
                table: "OrganicMediaAssets",
                column: "OrganicPostId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicPosts_WorkspaceId",
                schema: "social",
                table: "OrganicPosts",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicPosts_WorkspaceId_ScheduledAtUtc",
                schema: "social",
                table: "OrganicPosts",
                columns: new[] { "WorkspaceId", "ScheduledAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandVoiceProfiles",
                schema: "social");

            migrationBuilder.DropTable(
                name: "OrganicConnections",
                schema: "social");

            migrationBuilder.DropTable(
                name: "OrganicMediaAssets",
                schema: "social");

            migrationBuilder.DropTable(
                name: "OrganicPosts",
                schema: "social");
        }
    }
}
