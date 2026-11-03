using FluentAssertions;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Connections.Events;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes unitários do agregado <see cref="OrganicConnection"/>.
/// </summary>
public sealed class OrganicConnectionTests
{
    /// <summary>
    /// Valida criação bem-sucedida com dados válidos.
    /// </summary>
    [Fact]
    public void Create_ComDadosValidos_DeveCriarConexaoAtiva()
    {
        var workspaceId = Guid.NewGuid();

        var result = OrganicConnection.Create(
            workspaceId,
            OrganicPlatform.Instagram,
            "ig-123",
            "Conta Instagram",
            "publish,read",
            "cipher-access",
            "cipher-refresh",
            DateTime.UtcNow.AddDays(30));

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkspaceId.Should().Be(workspaceId);
        result.Value.Platform.Should().Be(OrganicPlatform.Instagram);
        result.Value.ExternalAccountId.Should().Be("ig-123");
        result.Value.DisplayName.Should().Be("Conta Instagram");
        result.Value.Scopes.Should().Be("publish,read");
        result.Value.Status.Should().Be(OrganicConnectionStatus.Active);
        result.Value.EncryptedAccessToken.Should().Be("cipher-access");
        result.Value.EncryptedRefreshToken.Should().Be("cipher-refresh");
    }

    /// <summary>
    /// Valida falha quando o workspace é vazio.
    /// </summary>
    [Fact]
    public void Create_ComWorkspaceVazio_DeveRetornarWorkspaceRequired()
    {
        var result = OrganicConnection.Create(
            Guid.Empty,
            OrganicPlatform.FacebookPage,
            "page-1",
            "Page",
            "scope",
            "cipher");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.WorkspaceRequired.Code);
    }

    /// <summary>
    /// Valida falha para valor de enum de plataforma não definido.
    /// </summary>
    [Fact]
    public void Create_ComPlataformaDesconhecida_DeveRetornarPlatformUnsupported()
    {
        var invalidPlatform = (OrganicPlatform)999;

        var result = OrganicConnection.Create(
            Guid.NewGuid(),
            invalidPlatform,
            "ext",
            "Nome",
            "scope",
            "cipher");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(SocialErrors.PlatformUnsupported.Code);
    }

    /// <summary>
    /// Valida que rotação de tokens em conexão revogada falha.
    /// </summary>
    [Fact]
    public void MarkTokensRotated_ComConexaoRevogada_DeveRetornarTokenRevoked()
    {
        var connection = CreateValidConnection();
        connection.Revoke().IsSuccess.Should().BeTrue();

        var before = connection.EncryptedAccessToken;
        var rotate = connection.MarkTokensRotated("new-cipher", null, DateTime.UtcNow.AddDays(1));

        rotate.IsFailure.Should().BeTrue();
        rotate.Error.Code.Should().Be(SocialErrors.TokenRevoked.Code);
        connection.EncryptedAccessToken.Should().Be(before);
    }

    /// <summary>
    /// Valida revogação idempotente e emissão única de evento.
    /// </summary>
    [Fact]
    public void Revoke_DeveSerIdempotente_EEmitirEventoUnico()
    {
        var connection = CreateValidConnection();

        connection.Revoke().IsSuccess.Should().BeTrue();
        connection.Status.Should().Be(OrganicConnectionStatus.Revoked);
        connection.DomainEvents.Should().ContainSingle(e => e is OrganicConnectionRevoked);

        var revokedEvent = (OrganicConnectionRevoked)connection.DomainEvents.Single();
        revokedEvent.ConnectionId.Should().Be(connection.Id);
        revokedEvent.WorkspaceId.Should().Be(connection.WorkspaceId);

        connection.Revoke().IsSuccess.Should().BeTrue();
        connection.DomainEvents.Should().ContainSingle(e => e is OrganicConnectionRevoked);
    }

    /// <summary>
    /// Garante que não existem propriedades públicas de token em claro.
    /// </summary>
    [Fact]
    public void PropriedadesPublicas_NaoDevemExporTokenEmClaro()
    {
        var properties = typeof(OrganicConnection)
            .GetProperties()
            .Where(p => p.Name.Contains("Token", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();

        properties.Should().BeEquivalentTo(["EncryptedAccessToken", "EncryptedRefreshToken", "AccessTokenExpiresAtUtc"]);
    }

    private static OrganicConnection CreateValidConnection()
    {
        return OrganicConnection.Create(
            Guid.NewGuid(),
            OrganicPlatform.TikTok,
            "tt-99",
            "TikTok Oficial",
            "video.publish",
            "cipher-access").Value;
    }
}
