using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Domain.Tenants;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para a entidade de log de notificações transacionais de tenant (<see cref="TenantNotificationLog"/>).
/// </summary>
public sealed class TenantNotificationLogTests
{
    /// <summary>
    /// Valida que um log de notificação válido é criado com sucesso.
    /// </summary>
    [Fact]
    public void Create_WhenValidInputs_ShouldCreateLogSuccessfully()
    {
        // Arrange
        var tenantId = TenantId.New();

        // Act
        var result = TenantNotificationLog.Create(
            tenantId,
            TrialNoticeType.WelcomeEmail,
            "carlos@agencia.com.br",
            "Bem-vindo ao AdMetricsPro",
            isSuccess: true,
            errorMessage: null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.Type.Should().Be(TrialNoticeType.WelcomeEmail);
        result.Value.RecipientEmail.Should().Be("carlos@agencia.com.br");
        result.Value.Subject.Should().Be("Bem-vindo ao AdMetricsPro");
        result.Value.IsSuccess.Should().BeTrue();
        result.Value.ErrorMessage.Should().BeNull();
        result.Value.SentAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Valida que a tentativa de criar log com e-mail inválido ou campos vazios falha.
    /// </summary>
    [Theory]
    [InlineData("", "Assunto")]
    [InlineData("email-invalido", "Assunto")]
    [InlineData("carlos@agencia.com.br", "")]
    public void Create_WhenInvalidInputs_ShouldReturnFailure(string email, string subject)
    {
        // Act
        var result = TenantNotificationLog.Create(
            TenantId.New(),
            TrialNoticeType.TrialReminder7Days,
            email,
            subject,
            isSuccess: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
