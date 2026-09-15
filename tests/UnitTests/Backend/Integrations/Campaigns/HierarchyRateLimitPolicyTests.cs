using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Infrastructure.Campaigns.Resilience;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para a política de rate limiting e backoff exponencial com jitter.
/// </summary>
public sealed class HierarchyRateLimitPolicyTests
{
    private readonly HierarchyRateLimitPolicy _policy = new(maxRetries: 3, baseDelayMs: 10);

    /// <summary>
    /// Valida que uma operação bem-sucedida na primeira tentativa é retornada sem atrasos de retentativa.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceedsFirstTime_ShouldReturnSuccess()
    {
        // Arrange
        var callCount = 0;

        // Act
        var result = await _policy.ExecuteAsync(ct =>
        {
            callCount++;
            return Task.FromResult(Result<string>.Success("Dados com sucesso"));
        }, "MetaAds");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Dados com sucesso");
        callCount.Should().Be(1);
    }

    /// <summary>
    /// Valida que a política aplica retentativa quando ocorre erro de limite de taxa (RateLimit.Exceeded) e recupera com sucesso.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenRateLimitOccursAndRecovers_ShouldSucceedAfterRetries()
    {
        // Arrange
        var callCount = 0;

        // Act
        var result = await _policy.ExecuteAsync(ct =>
        {
            callCount++;
            if (callCount < 3)
            {
                return Task.FromResult(Result<string>.Failure(
                    Error.Failure("RateLimit.Exceeded", "Muitas requisições para a Meta Ads API.")));
            }

            return Task.FromResult(Result<string>.Success("Sucesso na 3ª tentativa"));
        }, "MetaAds");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Sucesso na 3ª tentativa");
        callCount.Should().Be(3);
    }

    /// <summary>
    /// Valida que a política desiste e retorna erro quando o limite máximo de retentativas é superado.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenRateLimitExceedsMaxRetries_ShouldReturnFailure()
    {
        // Arrange
        var callCount = 0;

        // Act
        var result = await _policy.ExecuteAsync(ct =>
        {
            callCount++;
            return Task.FromResult(Result<string>.Failure(
                Error.Failure("RateLimit.QuotaExhausted", "Quota diária esgotada.")));
        }, "GoogleAds");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RateLimit.QuotaExhausted");
        callCount.Should().Be(4); // 1 chamada inicial + 3 retentativas
    }

    /// <summary>
    /// Valida que erros que não são de rate limit (ex: Validação ou Não Encontrado) não disparam retentativas desnecessárias.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenNonRetriableErrorOccurs_ShouldFailImmediately()
    {
        // Arrange
        var callCount = 0;

        // Act
        var result = await _policy.ExecuteAsync(ct =>
        {
            callCount++;
            return Task.FromResult(Result<string>.Failure(
                Error.NotFound("Campaign.NotFound", "Campanha externa inexistente.")));
        }, "TikTokAds");

        // Assert
        result.IsFailure.Should().BeTrue();
        callCount.Should().Be(1);
    }
}
