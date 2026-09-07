using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>
/// Migração que adiciona as tabelas de Squads, SquadMembers e SquadWorkspaces ao schema dos bancos operacionais de inquilinos.
/// </summary>
public partial class Add_Squads_And_Portfolio_Tables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Squads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Squads", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SquadMembers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SquadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SquadMembers", x => x.Id);
                table.ForeignKey(
                    name: "FK_SquadMembers_Squads_SquadId",
                    column: x => x.SquadId,
                    principalTable: "Squads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SquadWorkspaces",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SquadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SquadWorkspaces", x => x.Id);
                table.ForeignKey(
                    name: "FK_SquadWorkspaces_Squads_SquadId",
                    column: x => x.SquadId,
                    principalTable: "Squads",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Squads_Name",
            table: "Squads",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_Squads_IsActive",
            table: "Squads",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_SquadMembers_SquadId_UserId",
            table: "SquadMembers",
            columns: new[] { "SquadId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SquadMembers_UserId",
            table: "SquadMembers",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_SquadWorkspaces_SquadId_WorkspaceId",
            table: "SquadWorkspaces",
            columns: new[] { "SquadId", "WorkspaceId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SquadWorkspaces_WorkspaceId",
            table: "SquadWorkspaces",
            column: "WorkspaceId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SquadWorkspaces");

        migrationBuilder.DropTable(
            name: "SquadMembers");

        migrationBuilder.DropTable(
            name: "Squads");
    }
}
