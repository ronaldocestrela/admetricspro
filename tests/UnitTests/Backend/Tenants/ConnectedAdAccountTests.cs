using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para validação das invariantes de negócio da entidade <see cref="ConnectedAdAccount"/>.
/// </summary>
public sealed class ConnectedAdAccountTests
{
    /// <summary>
    /// Valida que a criação com parâmetros válidos instancia a conta com sucesso.
    /// </summary>
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var platform = "MetaAds";
        var externalAccountId = "act_123456789";
        var accountName = "Campanhas E-commerce";
        var currency = "BRL";
        var isDemo = false;

        // Act
        var result = ConnectedAdAccount.Create(
            id,
            workspaceId,
            platform,
            externalAccountId,
            accountName,
            currency,
            isDemo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal(workspaceId, result.Value.WorkspaceId);
        Assert.Equal(platform, result.Value.Platform);
        Assert.Equal(externalAccountId, result.Value.ExternalAccountId);
        Assert.Equal(accountName, result.Value.AccountName);
        Assert.Equal(currency, result.Value.Currency);
        Assert.False(result.Value.IsDemo);
        Assert.Equal("Connected", result.Value.Status);
    }

    /// <summary>
    /// Valida que a criação em modo demonstração instancia a conta com flag IsDemo ativa e nome personalizado.
    /// </summary>
    [Fact]
    public void CreateDemo_WithValidParameters_ShouldReturnSuccessWithIsDemoTrue()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var platform = "MetaAds";

        // Act
        var result = ConnectedAdAccount.CreateDemo(workspaceId, platform);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(workspaceId, result.Value.WorkspaceId);
        Assert.Equal(platform, result.Value.Platform);
        Assert.True(result.Value.IsDemo);
        Assert.Contains("Demonstração", result.Value.AccountName);
        Assert.Equal("Connected", result.Value.Status);
    }

    /// <summary>
    /// Valida que plataforma vazia ou nula retorna erro de validação.
    /// </summary>
    /// <param name="invalidPlatform">Plataforma nula, vazia ou inválida.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidPlatform_ShouldReturnFailure(string? invalidPlatform)
    {
        // Act
        var result = ConnectedAdAccount.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            invalidPlatform!,
            "act_123",
            "Conta Teste",
            "BRL",
            false);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ConnectedAdAccount.InvalidPlatform", result.Error.Code);
    }

    /// <summary>
    /// Valida que workspaceId vazio retorna erro de validação.
    /// </summary>
    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldReturnFailure()
    {
        // Act
        var result = ConnectedAdAccount.Create(
            Guid.NewGuid(),
            Guid.Empty,
            "MetaAds",
            "act_123",
            "Conta Teste",
            "BRL",
            false);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("ConnectedAdAccount.EmptyWorkspaceId", result.Error.Code);
    }
}
