using FluentAssertions;
using Master.Application.Emails;
using Master.Domain.Tenants;
using Xunit;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes unitários para a renderização de templates de e-mails transacionais (<see cref="TransactionalEmailTemplateRenderer"/>).
/// </summary>
public sealed class TransactionalEmailTemplateRendererTests
{
    private readonly ITransactionalEmailTemplateRenderer _renderer = new TransactionalEmailTemplateRenderer();

    /// <summary>
    /// Valida que o template de boas-vindas contém os links corretos de subdomínio e saudações personalizadas.
    /// </summary>
    [Fact]
    public void RenderWelcomeEmail_ShouldIncludePersonalizedGreetingAndSubdomainLoginUrl()
    {
        // Act
        var message = _renderer.RenderWelcomeEmail(
            recipientEmail: "carlos@agenciaalpha.com.br",
            recipientName: "Carlos Mendes",
            companyName: "Agência Alpha Digital",
            subdomain: "agencia-alpha",
            customDomain: null,
            tier: SubscriptionTier.Pro);

        // Assert
        message.To.Should().Be("carlos@agenciaalpha.com.br");
        message.Subject.Should().Contain("Bem-vindo ao AdMetricsPro");
        message.HtmlBody.Should().Contain("Carlos Mendes");
        message.HtmlBody.Should().Contain("Agência Alpha Digital");
        message.HtmlBody.Should().Contain("https://agencia-alpha.admetricspro.com.br/login");
        message.PlainTextBody.Should().Contain("https://agencia-alpha.admetricspro.com.br/login");
    }

    /// <summary>
    /// Valida que o template de boas-vindas prioriza o domínio customizado CNAME caso informado.
    /// </summary>
    [Fact]
    public void RenderWelcomeEmail_WhenCustomDomainProvided_ShouldUseCustomDomainInLoginUrl()
    {
        // Act
        var message = _renderer.RenderWelcomeEmail(
            recipientEmail: "carlos@agenciaalpha.com.br",
            recipientName: "Carlos Mendes",
            companyName: "Agência Alpha Digital",
            subdomain: "agencia-alpha",
            customDomain: "ads.agenciaalpha.com.br",
            tier: SubscriptionTier.Pro);

        // Assert
        message.HtmlBody.Should().Contain("https://ads.agenciaalpha.com.br/login");
        message.PlainTextBody.Should().Contain("https://ads.agenciaalpha.com.br/login");
    }

    /// <summary>
    /// Valida que os templates da régua de trial renderizam assuntos semânticos conforme o marco temporal.
    /// </summary>
    [Theory]
    [InlineData(TrialNoticeType.TrialReminder7Days, "7 dias")]
    [InlineData(TrialNoticeType.TrialReminder3Days, "3 dias")]
    [InlineData(TrialNoticeType.TrialReminder1Day, "1 dia")]
    [InlineData(TrialNoticeType.TrialExpired, "expirou")]
    public void RenderTrialNoticeEmail_ShouldRenderCorrectUrgencyInSubjectAndBody(
        TrialNoticeType noticeType,
        string expectedKeyword)
    {
        // Act
        var message = _renderer.RenderTrialNoticeEmail(
            recipientEmail: "carlos@agenciaalpha.com.br",
            recipientName: "Carlos Mendes",
            companyName: "Agência Alpha Digital",
            subdomain: "agencia-alpha",
            customDomain: null,
            noticeType: noticeType,
            expirationUtc: DateTime.UtcNow.AddDays(3),
            daysRemaining: 3.0);

        // Assert
        message.Subject.ToLower().Should().Contain(expectedKeyword);
        message.HtmlBody.Should().Contain("Carlos Mendes");
        message.HtmlBody.Should().Contain("https://agencia-alpha.admetricspro.com.br/login");
    }
}
