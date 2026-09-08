using FluentAssertions;
using Master.Domain.Tenants;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para a política pura de domínio da régua de lembretes de trial (<see cref="TrialNoticePolicy"/>).
/// </summary>
public sealed class TrialNoticePolicyTests
{
    private readonly DateTime _expirationUtc = new(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Valida que a mais de 7 dias da expiração nenhum lembrete é disparado.
    /// </summary>
    [Fact]
    public void EvaluatePendingNotice_WhenMoreThan7DaysRemaining_ShouldReturnNull()
    {
        // Arrange (8 dias antes)
        var referenceUtc = _expirationUtc.AddDays(-8);
        var sentNotices = new HashSet<TrialNoticeType>();

        // Act
        var result = TrialNoticePolicy.EvaluatePendingNotice(_expirationUtc, referenceUtc, sentNotices);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Valida que aos 7 dias antes da expiração o lembrete de 7 dias é emitido se ainda não enviado.
    /// </summary>
    [Fact]
    public void EvaluatePendingNotice_When7DaysRemainingAndNotSent_ShouldReturn7DaysNotice()
    {
        // Arrange (7 dias antes)
        var referenceUtc = _expirationUtc.AddDays(-7);
        var sentNotices = new HashSet<TrialNoticeType>();

        // Act
        var result = TrialNoticePolicy.EvaluatePendingNotice(_expirationUtc, referenceUtc, sentNotices);

        // Assert
        result.Should().Be(TrialNoticeType.TrialReminder7Days);
    }

    /// <summary>
    /// Valida que aos 7 dias antes da expiração, se o lembrete de 7 dias já foi enviado, nada pendente é retornado.
    /// </summary>
    [Fact]
    public void EvaluatePendingNotice_When7DaysRemainingAndAlreadySent_ShouldReturnNull()
    {
        // Arrange
        var referenceUtc = _expirationUtc.AddDays(-6);
        var sentNotices = new HashSet<TrialNoticeType> { TrialNoticeType.TrialReminder7Days };

        // Act
        var result = TrialNoticePolicy.EvaluatePendingNotice(_expirationUtc, referenceUtc, sentNotices);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Valida que aos 3 dias antes da expiração o lembrete de 3 dias é emitido se ainda não enviado.
    /// </summary>
    [Fact]
    public void EvaluatePendingNotice_When3DaysRemainingAndNotSent_ShouldReturn3DaysNotice()
    {
        // Arrange (3 dias antes)
        var referenceUtc = _expirationUtc.AddDays(-3);
        var sentNotices = new HashSet<TrialNoticeType> { TrialNoticeType.TrialReminder7Days };

        // Act
        var result = TrialNoticePolicy.EvaluatePendingNotice(_expirationUtc, referenceUtc, sentNotices);

        // Assert
        result.Should().Be(TrialNoticeType.TrialReminder3Days);
    }

    /// <summary>
    /// Valida que a 1 dia antes da expiração o lembrete de 1 dia é emitido se ainda não enviado.
    /// </summary>
    [Fact]
    public void EvaluatePendingNotice_When1DayRemainingAndNotSent_ShouldReturn1DayNotice()
    {
        // Arrange (1 dia antes)
        var referenceUtc = _expirationUtc.AddDays(-1);
        var sentNotices = new HashSet<TrialNoticeType>
        {
            TrialNoticeType.TrialReminder7Days,
            TrialNoticeType.TrialReminder3Days
        };

        // Act
        var result = TrialNoticePolicy.EvaluatePendingNotice(_expirationUtc, referenceUtc, sentNotices);

        // Assert
        result.Should().Be(TrialNoticeType.TrialReminder1Day);
    }

    /// <summary>
    /// Valida que após o vencimento do trial o lembrete de expiração é emitido se ainda não enviado.
    /// </summary>
    [Fact]
    public void EvaluatePendingNotice_WhenExpiredAndNotSent_ShouldReturnExpiredNotice()
    {
        // Arrange (já expirou há 1 hora)
        var referenceUtc = _expirationUtc.AddHours(1);
        var sentNotices = new HashSet<TrialNoticeType>
        {
            TrialNoticeType.TrialReminder7Days,
            TrialNoticeType.TrialReminder3Days,
            TrialNoticeType.TrialReminder1Day
        };

        // Act
        var result = TrialNoticePolicy.EvaluatePendingNotice(_expirationUtc, referenceUtc, sentNotices);

        // Assert
        result.Should().Be(TrialNoticeType.TrialExpired);
    }

    /// <summary>
    /// Valida que se todos os lembretes já foram enviados inclusive o de expiração, nenhum novo lembrete é emitido.
    /// </summary>
    [Fact]
    public void EvaluatePendingNotice_WhenAllSent_ShouldReturnNull()
    {
        // Arrange
        var referenceUtc = _expirationUtc.AddDays(1);
        var sentNotices = new HashSet<TrialNoticeType>
        {
            TrialNoticeType.TrialReminder7Days,
            TrialNoticeType.TrialReminder3Days,
            TrialNoticeType.TrialReminder1Day,
            TrialNoticeType.TrialExpired
        };

        // Act
        var result = TrialNoticePolicy.EvaluatePendingNotice(_expirationUtc, referenceUtc, sentNotices);

        // Assert
        result.Should().BeNull();
    }
}
