using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using NSubstitute;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Connections.DTOs;
using SocialPublishing.Application.Connections.Queries.ListOrganicConnections;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using Tenants.Application.Rbac.Attributes;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Application;

/// <summary>
/// Testes da consulta <see cref="ListOrganicConnectionsQueryHandler"/>.
/// </summary>
public sealed class ListOrganicConnectionsQueryTests
{
    private readonly IOrganicConnectionRepository _repository = Substitute.For<IOrganicConnectionRepository>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();

    /// <summary>
    /// A consulta exige permissão social.read no contrato MediatR.
    /// </summary>
    [Fact]
    public void Query_DeveExigirPermissaoSocialRead()
    {
        var attribute = typeof(ListOrganicConnectionsQuery)
            .GetCustomAttributes(typeof(RequireTenantPermissionAttribute), inherit: true)
            .Cast<RequireTenantPermissionAttribute>()
            .Single();

        attribute.Permission.Should().Be(TenantPermission.SocialRead);
    }

    /// <summary>
    /// Workspace vazio não consulta repositório.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceVazio_DeveFalharSemChamarRepositorio()
    {
        _tenantContextAccessor.TenantContext.Returns(
            TenantContext.Create(Guid.NewGuid(), null, TenantResolutionSource.Header));
        var handler = new ListOrganicConnectionsQueryHandler(_repository, _tenantContextAccessor);

        var result = await handler.Handle(new ListOrganicConnectionsQuery(Guid.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
        await _repository.DidNotReceive().ListByWorkspaceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sem tenant resolvido falha antes do repositório.
    /// </summary>
    [Fact]
    public async Task Handle_SemTenantContext_DeveFalharAntesDoRepositorio()
    {
        _tenantContextAccessor.TenantContext.Returns(TenantContext.Empty);
        var handler = new ListOrganicConnectionsQueryHandler(_repository, _tenantContextAccessor);

        var result = await handler.Handle(new ListOrganicConnectionsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.TenantRequired.Code);
        await _repository.DidNotReceive().ListByWorkspaceAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lista conexões sem expor propriedades de token.
    /// </summary>
    [Fact]
    public async Task Handle_ComConexoes_DeveRetornarResumoSemSegredos()
    {
        var workspaceId = Guid.NewGuid();
        _tenantContextAccessor.TenantContext.Returns(
            TenantContext.Create(Guid.NewGuid(), null, TenantResolutionSource.Header));
        var connection = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.Instagram,
            "ig-1",
            "Marca IG",
            "scope",
            "super-secret-ciphertext",
            "refresh-cipher",
            DateTime.UtcNow.AddDays(5)).Value;

        _repository.ListByWorkspaceAsync(workspaceId, Arg.Any<CancellationToken>())
            .Returns(new List<OrganicConnection> { connection });

        var handler = new ListOrganicConnectionsQueryHandler(_repository, _tenantContextAccessor);
        var result = await handler.Handle(new ListOrganicConnectionsQuery(workspaceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var dto = result.Value[0];
        dto.ConnectionId.Should().Be(connection.Id);
        dto.Platform.Should().Be(OrganicPlatform.Instagram);
        dto.DisplayName.Should().Be("Marca IG");

        var propertyNames = typeof(OrganicConnectionSummaryDto).GetProperties().Select(p => p.Name).ToList();
        propertyNames.Should().NotContain("EncryptedAccessToken");
        propertyNames.Should().NotContain("EncryptedRefreshToken");
        propertyNames.Should().NotContain("AccessToken");
    }
}
