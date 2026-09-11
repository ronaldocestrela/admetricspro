using Bunit;
using FluentAssertions;
using Tenants.Application.Audit.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Rbac;
using Xunit;

namespace UnitTests.Frontend.Components.Rbac;

/// <summary>
/// Testes unitários com bUnit para o componente <see cref="TenantAuditLogViewer"/>.
/// </summary>
public sealed class TenantAuditLogViewerTests : BunitTestBase
{
    /// <summary>
    /// Valida que a tabela de auditoria renderiza as colunas e registros com o formato esperado.
    /// </summary>
    [Fact]
    public void Render_ComLogsPreenchidos_DeveExibirTabelaEControlesDePaginacao()
    {
        // Arrange
        var logs = new List<TenantAuditLogDto>
        {
            new(
                Id: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                UserEmail: "admin@agencia.com",
                Action: "User.RoleChanged",
                Resource: "TenantUser",
                ResourceId: "123",
                Details: "Promovido a SquadLeader",
                IpAddress: "127.0.0.1",
                CreatedAtUtc: DateTime.UtcNow)
        };

        var response = new TenantAuditLogsResponse(logs, TotalCount: 1, Page: 1, PageSize: 20);

        // Act
        var cut = Render<TenantAuditLogViewer>(parameters =>
            parameters.Add(p => p.InitialLogs, response));

        // Assert
        cut.Find(".viewer-title").TextContent.Should().Contain("Trilha de Auditoria Imutável");
        cut.Find(".col-user").TextContent.Should().Be("admin@agencia.com");
        cut.Find(".action-pill").TextContent.Should().Be("User.RoleChanged");
        cut.Find(".col-details").TextContent.Should().Be("Promovido a SquadLeader");
        cut.Find(".total-info").TextContent.Should().Contain("1 registro(s)");
    }
}
