using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BrandVoiceBusinessAndVisualIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccentColor",
                schema: "social",
                table: "BrandVoiceProfiles",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessSummary",
                schema: "social",
                table: "BrandVoiceProfiles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Differentiators",
                schema: "social",
                table: "BrandVoiceProfiles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainOffer",
                schema: "social",
                table: "BrandVoiceProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                schema: "social",
                table: "BrandVoiceProfiles",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                schema: "social",
                table: "BrandVoiceProfiles",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetAudience",
                schema: "social",
                table: "BrandVoiceProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccentColor",
                schema: "social",
                table: "BrandVoiceProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessSummary",
                schema: "social",
                table: "BrandVoiceProfiles");

            migrationBuilder.DropColumn(
                name: "Differentiators",
                schema: "social",
                table: "BrandVoiceProfiles");

            migrationBuilder.DropColumn(
                name: "MainOffer",
                schema: "social",
                table: "BrandVoiceProfiles");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                schema: "social",
                table: "BrandVoiceProfiles");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                schema: "social",
                table: "BrandVoiceProfiles");

            migrationBuilder.DropColumn(
                name: "TargetAudience",
                schema: "social",
                table: "BrandVoiceProfiles");
        }
    }
}
