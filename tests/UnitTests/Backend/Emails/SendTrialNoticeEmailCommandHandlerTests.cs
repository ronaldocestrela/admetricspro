using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Emails;
using Master.Application.Emails.Commands.SendTrialNoticeEmail;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes do comando de envio de marco da régua de trial pelo consumidor de catálogo.
/// </summary>
public sealed class SendTrialNoticeEmailCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer = Substitute.For<ITransactionalEmailTemplateRenderer>();
    private readonly ITenantNotificationLogRepository _logRepository = Substitute.For<ITenantNotificationLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<SendTrialNoticeEmailCommandHandler> _logger = Substitute.For<ILogger<SendTrialNoticeEmailCommandHandler>>();

    private SendTrialNoticeEmailCommandHandler CreateHandler() => new(
        _tenantRepository,
        _emailSender,
        _templateRenderer,
        _logRepository,
        _unitOfWork,
        _logger);

    /// <summary>
    /// Segunda execução com log de sucesso não chama o sender.
    /// </summary>
    [Fact]
    public async Task Handle_WhenNoticeAlreadySent_ShouldNotCallEmailSender()
    {
        var reference = DateTime.UtcNow.AddDays(7);
        var tenant = Tenant.Create(
            "Ag",
            "12345678000190",
            "ag",
            subscriptionExpiresAtUtc: reference,
            adminEmail: "a@b.com").Value;

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType> { TrialNoticeType.TrialReminder7Days });

        var result = await CreateHandler().Handle(
            new SendTrialNoticeEmailCommand(Guid.NewGuid(), tenant.Id.Value, TrialNoticeType.TrialReminder7Days),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tenant ausente retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantMissing_ShouldReturnNotFound()
    {
        var tenantId = Guid.NewGuid();

        var result = await CreateHandler().Handle(
            new SendTrialNoticeEmailCommand(Guid.NewGuid(), tenantId, TrialNoticeType.TrialReminder3Days),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    /// <summary>
    /// Falha transitória de SMTP retorna <see cref="ITransientIntegrationFailure"/>.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTransientSmtpFailure_ShouldReturnTransientError()
    {
        var reference = DateTime.UtcNow.AddDays(3);
        var tenant = Tenant.Create(
            "Ag",
            "12345678000190",
            "ag",
            subscriptionExpiresAtUtc: reference,
            adminEmail: "a@b.com").Value;

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType>());

        var message = EmailMessage.Create("a@b.com", "Trial", "html", "text").Value;
        _templateRenderer.RenderTrialNoticeEmail(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<TrialNoticeType>(), Arg.Any<DateTime>(), Arg.Any<double>())
            .Returns(message);

        var transient = new EmailTransientSendFailure("timeout", DateTimeOffset.UtcNow.AddMinutes(1));
        _emailSender.SendEmailAsync(message, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(transient));

        var result = await CreateHandler().Handle(
            new SendTrialNoticeEmailCommand(Guid.NewGuid(), tenant.Id.Value, TrialNoticeType.TrialReminder3Days),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EmailTransientSendFailure>();
    }
}