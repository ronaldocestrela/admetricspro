using FluentAssertions;
using SocialPublishing.Domain.Publishing;
using Xunit;

namespace UnitTests.Backend.SocialPublishing.Domain;

/// <summary>
/// Testes da política pura de backoff para publicação orgânica.
/// </summary>
public sealed class OrganicPublishRateLimitPolicyTests
{
    private readonly OrganicPublishRateLimitPolicy _policy = new(baseDelayMs: 500, maxDelayMs: 10_000, maxAttempts: 3);

    /// <summary>
    /// Attempt 0 com jitter zero: 500ms.
    /// </summary>
    [Fact]
    public void CalculateDelayMs_Attempt0_SemJitter_Retorna500()
    {
        _policy.CalculateDelayMs(attempt: 0, jitterMs: 0).Should().Be(500);
    }

    /// <summary>
    /// Attempt 1: 2^1 * 500 = 1000ms.
    /// </summary>
    [Fact]
    public void CalculateDelayMs_Attempt1_SemJitter_Retorna1000()
    {
        _policy.CalculateDelayMs(attempt: 1, jitterMs: 0).Should().Be(1000);
    }

    /// <summary>
    /// Attempt 2: 2000ms.
    /// </summary>
    [Fact]
    public void CalculateDelayMs_Attempt2_SemJitter_Retorna2000()
    {
        _policy.CalculateDelayMs(attempt: 2, jitterMs: 0).Should().Be(2000);
    }

    /// <summary>
    /// Backoff alto é truncado no teto de 10 segundos antes de aplicar jitter.
    /// </summary>
    [Fact]
    public void CalculateDelayMs_AttemptAlto_TruncaEm10Segundos()
    {
        _policy.CalculateDelayMs(attempt: 10, jitterMs: 0).Should().Be(10_000);
    }

    /// <summary>
    /// Jitter é somado após o backoff base (ainda respeitando o teto).
    /// </summary>
    [Fact]
    public void CalculateDelayMs_ComJitter_SomaAntesDoTeto()
    {
        _policy.CalculateDelayMs(attempt: 0, jitterMs: 100).Should().Be(600);
    }

    /// <summary>
    /// Três tentativas permitidas: attemptCount 0 e 2 ainda restam; 3 esgota.
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void HasAttemptsRemaining_RespeitaMaxAttempts(int attemptCount, bool expected)
    {
        _policy.HasAttemptsRemaining(attemptCount).Should().Be(expected);
    }
}
