using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SocialPublishing.Infrastructure.Persistence;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adiciona a chave de objeto e o tipo de conteúdo da logo do cliente em <c>BrandVoiceProfiles</c>.
/// </summary>
[DbContext(typeof(SocialPublishingDbContext))]
[Migration("20261126130000_BrandVoiceClientLogo")]
public partial class BrandVoiceClientLogo : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LogoContentType",
            schema: "social",
            table: "BrandVoiceProfiles",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LogoObjectKey",
            schema: "social",
            table: "BrandVoiceProfiles",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LogoContentType",
            schema: "social",
            table: "BrandVoiceProfiles");

        migrationBuilder.DropColumn(
            name: "LogoObjectKey",
            schema: "social",
            table: "BrandVoiceProfiles");
    }
}