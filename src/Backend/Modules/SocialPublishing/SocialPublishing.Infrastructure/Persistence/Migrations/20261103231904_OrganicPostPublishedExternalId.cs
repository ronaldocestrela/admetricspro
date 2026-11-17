using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganicPostPublishedExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublishedExternalId",
                schema: "social",
                table: "OrganicPosts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishedExternalId",
                schema: "social",
                table: "OrganicPosts");
        }
    }
}
