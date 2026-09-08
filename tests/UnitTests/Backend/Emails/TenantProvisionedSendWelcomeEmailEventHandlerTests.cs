using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Application.Tenants.Events;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes unitários para o despachador de e-mail de boas-vindas (<see cref="TenantProvisionedSendWelcomeEmailEventHandler"/>).
/// </summary>
public sealed class TenantProvisionedSendWelcomeEmailEventHandlerTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer = Substitute.For<ITransactionalEmailTemplateRenderer>();
    private readonly ITenantNotificationLogRepository _logRepository = Substitute.For<ITenantNotificationLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<TenantProvisionedSendWelcomeEmailEventHandler> _logger = Substitute.For<ILogger<TenantProvisionedSendWelcomeEmailEventHandler>>();

    /// <summary>
    /// Valida que ao receber TenantProvisionedEvent o handler renderiza, envia o e-mail e registra o log com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEventReceived_ShouldRenderSendAndLogEmail()
    {
        // Arrange
        var tenantId = TenantId.New();
        var domainEvent = new TenantProvisionedEvent(
            tenantId,
            "Agência Alpha",
            "agencia-alpha",
            "carlos@alpha.com.br",
            "Carlos Mendes",
            null,
            SubscriptionTier.Pro,
            DateTime.UtcNow);

        var notification = new DomainEventNotification<TenantProvisionedEvent>(domainEvent);

        var sampleMessage = EmailMessage.Create("carlos@alpha.com.br", "Bem-vindo", "<p>Corpo</p>", "Corpo").Value;
        _templateRenderer.RenderWelcomeEmail(
            domainEvent.AdminEmail,
            domainEvent.AdminFullName,
            domainEvent.CompanyName,
            domainEvent.Subdomain,
            domainEvent.CustomDomain,
            domainEvent.Tier)
            .Returns(sampleMessage);

        _emailSender.SendEmailAsync(sampleMessage, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var handler = new TenantProvisionedSendWelcomeEmailEventHandler(
            _emailSender,
            _templateRenderer,
            _logRepository,
            _unitOfWork,
            _logger);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendEmailAsync(sampleMessage, Arg.Any<CancellationToken>());
        await _logRepository.Received(1).AddAsync(
            Arg.Is<TenantNotificationLog>(log => log.TenantId == tenantId && log.IsSuccess && log.Type == TrialNoticeType.WelcomeEmail),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que quando o envio de e-mail falha, o log registra a falha sem estourar exceção.
    /// </summary>
    [Fact]
    public async Task Handle_WhenEmailSendFails_ShouldRecordFailureInLogWithoutThrowing()
    {
        // Arrange
        var tenantId = TenantId.New();
        var domainEvent = new TenantProvisionedEvent(
            tenantId,
            "Agência Alpha",
            "agencia-alpha",
            "carlos@alpha.com.br",
            "Carlos Mendes",
            null,
            SubscriptionTier.Pro,
            DateTime.UtcNow);

        var notification = new DomainEventNotification<TenantProvisionedEvent>(domainEvent);

        var sampleMessage = EmailMessage.Create("carlos@alpha.com.br", "Bem-vindo", "<p>Corpo</p>", "Corpo").Value;
        _templateRenderer.RenderWelcomeEmail(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<SubscriptionTier>())
            .Returns(sampleMessage);

        _emailSender.SendEmailAsync(sampleMessage, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(EmailErrors.SendFailed("Servidor SMTP fora do ar")));

        var handler = new TenantProvisionedSendWelcomeEmailEventHandler(
            _emailSender,
            _templateRenderer,
            _logRepository,
            _unitOfWork,
            _logger);

        // Act
        var act = () => handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        await _logRepository.Received(1).AddAsync(
            Arg.Is<TenantNotificationLog>(log => log.TenantId == tenantId && !log.IsSuccess && log.ErrorMessage!.Contains("SMTP")),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
