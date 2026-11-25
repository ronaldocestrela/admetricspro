using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Emails;
using Master.Application.Emails.Commands.SendDunningStageEmail;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes do comando de envio de e-mail de estágio de inadimplência pelo consumidor de catálogo.
/// </summary>
public sealed class SendDunningStageEmailCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer = Substitute.For<ITransactionalEmailTemplateRenderer>();
    private readonly ITenantNotificationLogRepository _logRepository = Substitute.For<ITenantNotificationLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<SendDunningStageEmailCommandHandler> _logger = Substitute.For<ILogger<SendDunningStageEmailCommandHandler>>();

    private SendDunningStageEmailCommandHandler CreateHandler() => new(
        _tenantRepository,
        _emailSender,
        _templateRenderer,
        _logRepository,
        _unitOfWork,
        _logger);

    /// <summary>
    /// Episódio já notificado com sucesso não reenvia.
    /// </summary>
    [Fact]
    public async Task Handle_WhenAlreadySentForEpisode_ShouldNotCallEmailSender()
    {
        var due = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var tenant = Tenant.Create("Ag", "12345678000190", "ag", adminEmail: "a@b.com").Value;
        tenant.MarkPaymentOverdue(due);

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _logRepository.HasSuccessfulNoticeSinceAsync(
                tenant.Id,
                TrialNoticeType.DunningAutomationsDisabled,
                due,
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(
            new SendDunningStageEmailCommand(
                Guid.NewGuid(),
                tenant.Id.Value,
                DunningStage.AutomationsDisabled,
                due),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Novo episódio com vencimento posterior envia novamente.
    /// </summary>
    [Fact]
    public async Task Handle_WhenNewPaymentEpisode_ShouldSendEmail()
    {
        var previousDue = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDue = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var tenant = Tenant.Create("Ag", "12345678000190", "ag", adminEmail: "a@b.com").Value;
        tenant.MarkPaymentOverdue(newDue);

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _logRepository.HasSuccessfulNoticeSinceAsync(
                tenant.Id,
                TrialNoticeType.DunningAutomationsDisabled,
                newDue,
                Arg.Any<CancellationToken>())
            .Returns(false);

        var message = EmailMessage.Create("a@b.com", "Dunning", "html", "text").Value;
        _templateRenderer.RenderDunningStageEmail(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<DunningStage>(), Arg.Any<DateTime>(), Arg.Any<int>())
            .Returns(message);
        _emailSender.SendEmailAsync(message, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await CreateHandler().Handle(
            new SendDunningStageEmailCommand(
                Guid.NewGuid(),
                tenant.Id.Value,
                DunningStage.AutomationsDisabled,
                newDue),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailSender.Received(1).SendEmailAsync(message, Arg.Any<CancellationToken>());
        await _logRepository.Received(1).AddAsync(Arg.Any<TenantNotificationLog>(), Arg.Any<CancellationToken>());
        _ = previousDue;
    }
}