using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Emails;
using Master.Application.Emails.Commands.SendTenantWelcomeEmail;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes do comando de e-mail de boas-vindas disparado pelo consumidor de catálogo.
/// </summary>
public sealed class SendTenantWelcomeEmailCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer = Substitute.For<ITransactionalEmailTemplateRenderer>();
    private readonly ITenantNotificationLogRepository _logRepository = Substitute.For<ITenantNotificationLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<SendTenantWelcomeEmailCommandHandler> _logger = Substitute.For<ILogger<SendTenantWelcomeEmailCommandHandler>>();

    private SendTenantWelcomeEmailCommandHandler CreateHandler() => new(
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
    public async Task Handle_WhenWelcomeAlreadySent_ShouldNotCallEmailSender()
    {
        var tenant = Tenant.Create("Ag", "12345678000190", "ag", adminEmail: "a@b.com").Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType> { TrialNoticeType.WelcomeEmail });

        var result = await CreateHandler().Handle(
            new SendTenantWelcomeEmailCommand(Guid.NewGuid(), tenant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Destinatário inválido retorna validação após registrar log de falha.
    /// </summary>
    [Fact]
    public async Task Handle_WhenSendReturnsValidation_ShouldReturnValidationError()
    {
        var tenant = Tenant.Create("Ag", "12345678000190", "ag", adminEmail: "a@b.com").Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType>());

        var message = EmailMessage.Create("a@b.com", "Bem-vindo", "html", "text").Value;
        _templateRenderer.RenderWelcomeEmail(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<SubscriptionTier>())
            .Returns(message);
        _emailSender.SendEmailAsync(message, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(EmailErrors.InvalidRecipient));

        var result = await CreateHandler().Handle(
            new SendTenantWelcomeEmailCommand(Guid.NewGuid(), tenant.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await _logRepository.Received(1).AddAsync(
            Arg.Is<TenantNotificationLog>(l => !l.IsSuccess),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Falha transitória de SMTP retorna <see cref="ITransientIntegrationFailure"/>.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTransientSmtpFailure_ShouldReturnTransientError()
    {
        var tenant = Tenant.Create("Ag", "12345678000190", "ag", adminEmail: "a@b.com").Value;
        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType>());

        var message = EmailMessage.Create("a@b.com", "Bem-vindo", "html", "text").Value;
        _templateRenderer.RenderWelcomeEmail(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<SubscriptionTier>())
            .Returns(message);

        var transient = new EmailTransientSendFailure("timeout", DateTimeOffset.UtcNow.AddMinutes(1));
        _emailSender.SendEmailAsync(message, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(transient));

        var result = await CreateHandler().Handle(
            new SendTenantWelcomeEmailCommand(Guid.NewGuid(), tenant.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<EmailTransientSendFailure>();
    }
}