using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganicMediaAssetMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OrganicPostId",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "AspectLabel",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMilliseconds",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganicMediaAssets_WorkspaceId",
                schema: "social",
                table: "OrganicMediaAssets",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganicMediaAssets_WorkspaceId",
                schema: "social",
                table: "OrganicMediaAssets");

            migrationBuilder.DropColumn(
                name: "AspectLabel",
                schema: "social",
                table: "OrganicMediaAssets");

            migrationBuilder.DropColumn(
                name: "DurationMilliseconds",
                schema: "social",
                table: "OrganicMediaAssets");

            migrationBuilder.DropColumn(
                name: "Format",
                schema: "social",
                table: "OrganicMediaAssets");

            migrationBuilder.DropColumn(
                name: "MimeType",
                schema: "social",
                table: "OrganicMediaAssets");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                schema: "social",
                table: "OrganicMediaAssets");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                schema: "social",
                table: "OrganicMediaAssets");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganicPostId",
                schema: "social",
                table: "OrganicMediaAssets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
