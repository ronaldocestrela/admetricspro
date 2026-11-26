using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational
{
    /// <inheritdoc />
    public partial class Add_TenantBranding_ObjectKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DarkLogoContentType",
                table: "TenantBranding",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DarkLogoObjectKey",
                table: "TenantBranding",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaviconContentType",
                table: "TenantBranding",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaviconObjectKey",
                table: "TenantBranding",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LightLogoContentType",
                table: "TenantBranding",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LightLogoObjectKey",
                table: "TenantBranding",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DarkLogoContentType", table: "TenantBranding");
            migrationBuilder.DropColumn(name: "DarkLogoObjectKey", table: "TenantBranding");
            migrationBuilder.DropColumn(name: "FaviconContentType", table: "TenantBranding");
            migrationBuilder.DropColumn(name: "FaviconObjectKey", table: "TenantBranding");
            migrationBuilder.DropColumn(name: "LightLogoContentType", table: "TenantBranding");
            migrationBuilder.DropColumn(name: "LightLogoObjectKey", table: "TenantBranding");
        }
    }
}