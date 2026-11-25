using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Trial;
using Master.Application.Emails;
using Master.Application.Messaging;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes unitários para o motor de notificações do ciclo de vida de trial (<see cref="TrialNotificationEngineService"/>).
/// </summary>
public sealed class TrialNotificationEngineServiceTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly ITenantNotificationLogRepository _logRepository = Substitute.For<ITenantNotificationLogRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer = Substitute.For<ITransactionalEmailTemplateRenderer>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ILogger<TrialNotificationEngineService> _logger = Substitute.For<ILogger<TrialNotificationEngineService>>();
    private readonly IMessagingFeatureGate _messagingGate = Substitute.For<IMessagingFeatureGate>();
    private readonly ICatalogIntegrationOutboxWriter _catalogOutbox = Substitute.For<ICatalogIntegrationOutboxWriter>();

    private TrialNotificationEngineService CreateEngine() => new(
        _tenantRepository,
        _logRepository,
        _emailSender,
        _templateRenderer,
        _unitOfWork,
        _publisher,
        _logger,
        _messagingGate,
        _catalogOutbox);

  /// <summary>
  /// Valida que tenants elegíveis com marcos pendentes são processados e contabilizados no sumário.
  /// </summary>
    [Fact]
    public async Task ProcessTrialNoticesCycleAsync_WhenPendingNoticesExist_ShouldSendEmailsAndReturnSummary()
    {
        _messagingGate.IsEnabled.Returns(false);

        var referenceUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var expirationUtc = referenceUtc.AddDays(7);

        var tenant = Tenant.Create(
            "Agência Beta",
            "12345678000195",
            "agencia-beta",
            SubscriptionTier.Trial,
            subscriptionExpiresAtUtc: expirationUtc,
            adminEmail: "beta@agenciabeta.com.br",
            adminFullName: "Roberto Lima").Value;

        _tenantRepository.GetTenantsForTrialNoticeEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant });

        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType>());

        var sampleMessage = EmailMessage.Create("beta@agenciabeta.com.br", "Assunto", "<p>HTML</p>", "Texto").Value;
        _templateRenderer.RenderTrialNoticeEmail(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<TrialNoticeType>(), Arg.Any<DateTime>(), Arg.Any<double>())
            .Returns(sampleMessage);

        _emailSender.SendEmailAsync(sampleMessage, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateEngine().ProcessTrialNoticesCycleAsync(referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCount.Should().Be(1);
        result.Value.SevenDayNoticesSent.Should().Be(1);
        result.Value.ThreeDayNoticesSent.Should().Be(0);
        result.Value.OneDayNoticesSent.Should().Be(0);
        result.Value.ExpiredNoticesSent.Should().Be(0);
        result.Value.FailuresCount.Should().Be(0);

        await _emailSender.Received(1).SendEmailAsync(sampleMessage, Arg.Any<CancellationToken>());
        await _logRepository.Received(1).AddAsync(Arg.Any<TenantNotificationLog>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que tenants cujos avisos daquele marco temporal já foram enviados são ignorados sem novo disparo.
    /// </summary>
    [Fact]
    public async Task ProcessTrialNoticesCycleAsync_WhenAlreadySent_ShouldSkipNotice()
    {
        _messagingGate.IsEnabled.Returns(false);

        var referenceUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var expirationUtc = referenceUtc.AddDays(7);

        var tenant = Tenant.Create(
            "Agência Beta",
            "12345678000195",
            "agencia-beta",
            SubscriptionTier.Trial,
            subscriptionExpiresAtUtc: expirationUtc,
            adminEmail: "beta@agenciabeta.com.br",
            adminFullName: "Roberto Lima").Value;

        _tenantRepository.GetTenantsForTrialNoticeEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant });

        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType> { TrialNoticeType.TrialReminder7Days });

        var result = await CreateEngine().ProcessTrialNoticesCycleAsync(referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EvaluatedCount.Should().Be(1);
        result.Value.TotalNoticesSent.Should().Be(0);

        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Com mensageria ligada, três tenants na janela gravam outbox e não chamam SMTP.
    /// </summary>
    [Fact]
    public async Task ProcessTrialNoticesCycleAsync_WhenMessagingEnabled_ShouldEnqueueOutboxesWithoutSmtp()
    {
        _messagingGate.IsEnabled.Returns(true);
        _catalogOutbox.AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var referenceUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);

        var tenant7 = CreateTrialTenant("t7", referenceUtc.AddDays(7));
        var tenant3 = CreateTrialTenant("t3", referenceUtc.AddDays(3));
        var tenant1 = CreateTrialTenant("t1", referenceUtc.AddDays(1));
        var tenantOut = CreateTrialTenant("out", referenceUtc.AddDays(10));

        _tenantRepository.GetTenantsForTrialNoticeEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant7, tenant3, tenant1, tenantOut });

        foreach (var tenant in new[] { tenant7, tenant3, tenant1, tenantOut })
        {
            _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
                .Returns(new HashSet<TrialNoticeType>());
        }

        var result = await CreateEngine().ProcessTrialNoticesCycleAsync(referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SevenDayNoticesSent.Should().Be(1);
        result.Value.ThreeDayNoticesSent.Should().Be(1);
        result.Value.OneDayNoticesSent.Should().Be(1);
        result.Value.TotalNoticesSent.Should().Be(3);

        await _catalogOutbox.Received(3).AddAsync(
            Arg.Is<IntegrationOutboxMessage>(m => m.MessageType == CatalogIntegrationMessageTypes.TrialNoticeEmail),
            Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _logRepository.DidNotReceive().AddAsync(Arg.Any<TenantNotificationLog>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Reexecução do ciclo com EventId duplicado não falha o lote nem chama SMTP.
    /// </summary>
    [Fact]
    public async Task ProcessTrialNoticesCycleAsync_WhenDuplicateEventId_ShouldTreatAsAlreadyEnqueued()
    {
        _messagingGate.IsEnabled.Returns(true);
        var duplicate = Error.Conflict("Messaging.DuplicateEventId", "Já existe outbox para este EventId.");
        _catalogOutbox.AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(duplicate));

        var referenceUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var tenant = CreateTrialTenant("dup", referenceUtc.AddDays(7));

        _tenantRepository.GetTenantsForTrialNoticeEvaluationAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Tenant> { tenant });
        _logRepository.GetSentNoticeTypesForTenantAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(new HashSet<TrialNoticeType>());

        var result = await CreateEngine().ProcessTrialNoticesCycleAsync(referenceUtc, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SevenDayNoticesSent.Should().Be(1);
        result.Value.FailuresCount.Should().Be(0);
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    private static Tenant CreateTrialTenant(string subdomain, DateTime expirationUtc) =>
        Tenant.Create(
            $"Agência {subdomain}",
            "12345678000195",
            subdomain,
            SubscriptionTier.Trial,
            subscriptionExpiresAtUtc: expirationUtc,
            adminEmail: $"{subdomain}@example.com").Value;
}