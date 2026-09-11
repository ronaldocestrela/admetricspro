using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Audit.DTOs;
using Tenants.Application.Audit.Queries.GetTenantAuditLogs;
using Tenants.Application.Audit.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="GetTenantAuditLogsQueryHandler"/> validando a listagem
/// paginada e filtrada de logs de auditoria do inquilino.
/// </summary>
public sealed class GetTenantAuditLogsQueryHandlerTests
{
    private readonly ITenantAuditLogRepository _auditRepository = Substitute.For<ITenantAuditLogRepository>();
    private readonly GetTenantAuditLogsQueryHandler _handler;

    /// <summary>
    /// Inicializa a suíte de testes com os mocks das dependências.
    /// </summary>
    public GetTenantAuditLogsQueryHandlerTests()
    {
        _handler = new GetTenantAuditLogsQueryHandler(_auditRepository);
    }

    /// <summary>
    /// Valida que a consulta retorna a lista paginada e a contagem total de logs de auditoria.
    /// </summary>
    [Fact]
    public async Task Handle_ComFiltrosValidos_DeveRetornarListaPaginadaEDTOs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<TenantAuditLog>
        {
            TenantAuditLog.Create(
                Guid.NewGuid(),
                userId,
                "admin@agencia.com",
                "User.RoleChanged",
                "TenantUser",
                "1",
                "Alterado",
                "127.0.0.1",
                DateTime.UtcNow).Value
        };

        _auditRepository.GetLogsAsync(
            userId,
            "User.RoleChanged",
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            page: 1,
            pageSize: 10,
            Arg.Any<CancellationToken>()).Returns(logs);

        _auditRepository.GetTotalCountAsync(
            userId,
            "User.RoleChanged",
            Arg.Any<DateTime?>(),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>()).Returns(1);

        var query = new GetTenantAuditLogsQuery(
            UserId: userId,
            Action: "User.RoleChanged",
            FromUtc: null,
            ToUtc: null,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].UserEmail.Should().Be("admin@agencia.com");
        result.Value.Items[0].Action.Should().Be("User.RoleChanged");
    }
}
