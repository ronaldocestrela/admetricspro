using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Suíte de testes unitários para a entidade imutável de auditoria <see cref="TenantAuditLog"/>,
/// validando criação com sucesso, validação de invariantes e imutabilidade de registros operacionais.
/// </summary>
public sealed class TenantAuditLogTests
{
    /// <summary>
    /// Valida que um registro de auditoria é criado com sucesso quando todos os dados obrigatórios são válidos.
    /// </summary>
    [Fact]
    public void Create_ComDadosValidos_DeveInstanciarComSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userEmail = "admin@agencia.com";
        var action = "User.RoleChanged";
        var resource = "TenantUser";
        var resourceId = Guid.NewGuid().ToString();
        var details = "Papel alterado de MediaManager para SquadLeader";
        var ipAddress = "192.168.1.100";
        var createdAtUtc = DateTime.UtcNow;

        // Act
        var result = TenantAuditLog.Create(
            id,
            userId,
            userEmail,
            action,
            resource,
            resourceId,
            details,
            ipAddress,
            createdAtUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var entry = result.Value;
        entry.Id.Should().Be(id);
        entry.UserId.Should().Be(userId);
        entry.UserEmail.Should().Be(userEmail);
        entry.Action.Should().Be(action);
        entry.Resource.Should().Be(resource);
        entry.ResourceId.Should().Be(resourceId);
        entry.Details.Should().Be(details);
        entry.IpAddress.Should().Be(ipAddress);
        entry.CreatedAtUtc.Should().Be(createdAtUtc);
    }

    /// <summary>
    /// Valida que a criação falha se o identificador do log for vazio.
    /// </summary>
    [Fact]
    public void Create_ComIdVazio_DeveRetornarFalhaDeValidacao()
    {
        // Act
        var result = TenantAuditLog.Create(
            Guid.Empty,
            Guid.NewGuid(),
            "admin@agencia.com",
            "User.RoleChanged",
            "TenantUser",
            "123",
            "Details",
            "127.0.0.1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantAuditLog.InvalidId");
    }

    /// <summary>
    /// Valida que a criação falha se a ação for nula ou vazia.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComAcaoInvalida_DeveRetornarFalhaDeValidacao(string? invalidAction)
    {
        // Act
        var result = TenantAuditLog.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "admin@agencia.com",
            invalidAction!,
            "TenantUser",
            "123",
            "Details",
            "127.0.0.1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantAuditLog.InvalidAction");
    }

    /// <summary>
    /// Valida que a criação falha se o email for inválido ou vazio.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComEmailInvalido_DeveRetornarFalhaDeValidacao(string? invalidEmail)
    {
        // Act
        var result = TenantAuditLog.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            invalidEmail!,
            "User.RoleChanged",
            "TenantUser",
            "123",
            "Details",
            "127.0.0.1");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantAuditLog.InvalidUserEmail");
    }
}
