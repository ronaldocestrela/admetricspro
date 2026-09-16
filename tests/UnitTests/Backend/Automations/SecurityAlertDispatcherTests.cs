using Automations.Domain.SafetyGuards;
using Automations.Infrastructure.SafetyGuards;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o despachador de alertas SecurityAlertDispatcher (Subfase 4.2.2 - TDD).
/// Valida o envio coordenado e resiliente para múltiplos canais (Slack, WhatsApp, E-mail, Webhook).
/// </summary>
public sealed class SecurityAlertDispatcherTests
{
    private readonly ISlackWebhookNotifier _slackNotifier = Substitute.For<ISlackWebhookNotifier>();
    private readonly IWhatsAppWebhookNotifier _whatsappNotifier = Substitute.For<IWhatsAppWebhookNotifier>();
    private readonly IEmailAlertNotifier _emailNotifier = Substitute.For<IEmailAlertNotifier>();
    private readonly IGenericWebhookNotifier _webhookNotifier = Substitute.For<IGenericWebhookNotifier>();

    private readonly SafetyAlertPayload _samplePayload = new(
        IncidentId: Guid.NewGuid(),
        WorkspaceId: Guid.NewGuid(),
        GuardType: SafetyGuardType.Overspending,
        Severity: SafetyAlertSeverity.Critical,
        Title: "🚨 ALERTA CRÍTICO: Overspending Detectado na Campanha 'Campanha Top'",
        Message: "Gasto diário superou 120% do orçamento programado.",
        TargetEntityName: "Campanha Top",
        TargetEntityId: Guid.NewGuid(),
        Platform: "MetaAds",
        ActionTaken: "Campanha Pausada Preventivamente",
        MetricsContext: new Dictionary<string, string>
        {
            { "CurrentSpend", "150.00" },
            { "DailyBudget", "100.00" },
            { "Percentage", "150.0%" }
        },
        TimestampUtc: DateTime.UtcNow);

    /// <summary>
    /// Valida que o dispatcher envia para todos os canais configurados com sucesso.
    /// </summary>
    [Fact]
    public async Task DispatchAlertAsync_ShouldSendToAllConfiguredChannels_WhenAllSucceed()
    {
        // Arrange
        _slackNotifier.SendSlackAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _whatsappNotifier.SendWhatsAppAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _emailNotifier.SendEmailAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _webhookNotifier.SendWebhookAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var options = new SecurityAlertNotificationOptions
        {
            SlackWebhookUrl = "https://hooks.slack.com/services/T00/B00/X00",
            WhatsAppRecipientPhone = "+5511999998888",
            AlertRecipientEmail = "alerta@admetricspro.com",
            GenericWebhookUrl = "https://api.empresa.com/webhooks/security"
        };

        var dispatcher = new SecurityAlertDispatcher(
            _slackNotifier,
            _whatsappNotifier,
            _emailNotifier,
            _webhookNotifier,
            options);

        // Act
        var result = await dispatcher.DispatchAlertAsync(_samplePayload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[SafetyAlertChannel.Slack].Should().BeTrue();
        result.Value[SafetyAlertChannel.WhatsApp].Should().BeTrue();
        result.Value[SafetyAlertChannel.Email].Should().BeTrue();
        result.Value[SafetyAlertChannel.Webhook].Should().BeTrue();

        await _slackNotifier.Received(1).SendSlackAlertAsync(options.SlackWebhookUrl, _samplePayload, Arg.Any<CancellationToken>());
        await _whatsappNotifier.Received(1).SendWhatsAppAlertAsync(options.WhatsAppRecipientPhone, _samplePayload, Arg.Any<CancellationToken>());
        await _emailNotifier.Received(1).SendEmailAlertAsync(options.AlertRecipientEmail, _samplePayload, Arg.Any<CancellationToken>());
        await _webhookNotifier.Received(1).SendWebhookAlertAsync(options.GenericWebhookUrl, _samplePayload, options.GenericWebhookSecret, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a falha em um canal (ex: Slack) não impede o envio para os demais canais (WhatsApp, E-mail, Webhook).
    /// </summary>
    [Fact]
    public async Task DispatchAlertAsync_ShouldContinueSendingToOtherChannels_WhenOneChannelFails()
    {
        // Arrange: Slack falha com erro de conexão, os demais têm sucesso
        _slackNotifier.SendSlackAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(Error.Failure("Slack.HttpError", "Falha HTTP no Slack"))));

        _whatsappNotifier.SendWhatsAppAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _emailNotifier.SendEmailAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        _webhookNotifier.SendWebhookAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var options = new SecurityAlertNotificationOptions
        {
            SlackWebhookUrl = "https://hooks.slack.com/services/T00/B00/X00",
            WhatsAppRecipientPhone = "+5511999998888",
            AlertRecipientEmail = "alerta@admetricspro.com",
            GenericWebhookUrl = "https://api.empresa.com/webhooks/security"
        };

        var dispatcher = new SecurityAlertDispatcher(
            _slackNotifier,
            _whatsappNotifier,
            _emailNotifier,
            _webhookNotifier,
            options);

        // Act
        var result = await dispatcher.DispatchAlertAsync(_samplePayload);

        // Assert: A operação geral ainda retorna sucesso com relatório granular de canais
        result.IsSuccess.Should().BeTrue();
        result.Value[SafetyAlertChannel.Slack].Should().BeFalse();
        result.Value[SafetyAlertChannel.WhatsApp].Should().BeTrue();
        result.Value[SafetyAlertChannel.Email].Should().BeTrue();
        result.Value[SafetyAlertChannel.Webhook].Should().BeTrue();

        await _whatsappNotifier.Received(1).SendWhatsAppAlertAsync(options.WhatsAppRecipientPhone, _samplePayload, Arg.Any<CancellationToken>());
        await _emailNotifier.Received(1).SendEmailAlertAsync(options.AlertRecipientEmail, _samplePayload, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que canais não configurados (URL/telefone vazios) são ignorados sem gerar erros.
    /// </summary>
    [Fact]
    public async Task DispatchAlertAsync_ShouldSkipUnconfiguredChannels()
    {
        // Arrange: Apenas E-mail configurado
        _emailNotifier.SendEmailAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var options = new SecurityAlertNotificationOptions
        {
            AlertRecipientEmail = "alerta@admetricspro.com"
        };

        var dispatcher = new SecurityAlertDispatcher(
            _slackNotifier,
            _whatsappNotifier,
            _emailNotifier,
            _webhookNotifier,
            options);

        // Act
        var result = await dispatcher.DispatchAlertAsync(_samplePayload);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ContainsKey(SafetyAlertChannel.Slack).Should().BeFalse();
        result.Value.ContainsKey(SafetyAlertChannel.WhatsApp).Should().BeFalse();
        result.Value.ContainsKey(SafetyAlertChannel.Webhook).Should().BeFalse();
        result.Value[SafetyAlertChannel.Email].Should().BeTrue();

        await _slackNotifier.DidNotReceive().SendSlackAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>());
        await _whatsappNotifier.DidNotReceive().SendWhatsAppAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<CancellationToken>());
        await _webhookNotifier.DidNotReceive().SendWebhookAlertAsync(Arg.Any<string>(), Arg.Any<SafetyAlertPayload>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
