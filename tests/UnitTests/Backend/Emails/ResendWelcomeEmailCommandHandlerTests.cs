using BuildingBlocks.Application.Emails;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Emails;
using Master.Application.Repositories;
using Master.Application.Tenants.Commands.ResendWelcomeEmail;
using Master.Domain.Tenants;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Emails;

/// <summary>
/// Testes unitários para o manipulador de comando <see cref="ResendWelcomeEmailCommandHandler"/>.
/// </summary>
public sealed class ResendWelcomeEmailCommandHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly ITransactionalEmailTemplateRenderer _templateRenderer = Substitute.For<ITransactionalEmailTemplateRenderer>();
    private readonly ITenantNotificationLogRepository _logRepository = Substitute.For<ITenantNotificationLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ILogger<ResendWelcomeEmailCommandHandler> _logger = Substitute.For<ILogger<ResendWelcomeEmailCommandHandler>>();

    /// <summary>
    /// Valida que o reenvio de e-mail com inquilino existente renderiza, envia e retorna sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantExists_ShouldResendWelcomeEmailSuccessfully()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Vanguarda Digital",
            "12345678000195",
            "vanguarda",
            SubscriptionTier.Pro,
            adminEmail: "carlos@vanguarda.com.br",
            adminFullName: "Carlos Mendes").Value;

        _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>())
            .Returns(tenant);

        var sampleMessage = EmailMessage.Create("carlos@vanguarda.com.br", "Assunto", "<p>Corpo</p>", "Corpo").Value;
        _templateRenderer.RenderWelcomeEmail(
            tenant.AdminEmail!,
            tenant.AdminFullName!,
            tenant.CompanyName,
            tenant.Subdomain,
            tenant.CustomDomain,
            tenant.Tier)
            .Returns(sampleMessage);

        _emailSender.SendEmailAsync(sampleMessage, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var handler = new ResendWelcomeEmailCommandHandler(
            _tenantRepository,
            _emailSender,
            _templateRenderer,
            _logRepository,
            _unitOfWork,
            _logger);

        var command = new ResendWelcomeEmailCommand(tenant.Id.Value);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        await _emailSender.Received(1).SendEmailAsync(sampleMessage, Arg.Any<CancellationToken>());
        await _logRepository.Received(1).AddAsync(Arg.Any<TenantNotificationLog>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao informar um Tenant inexistente o handler retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var missingTenantId = Guid.NewGuid();
        _tenantRepository.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var handler = new ResendWelcomeEmailCommandHandler(
            _tenantRepository,
            _emailSender,
            _templateRenderer,
            _logRepository,
            _unitOfWork,
            _logger);

        var command = new ResendWelcomeEmailCommand(missingTenantId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        await _emailSender.DidNotReceive().SendEmailAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
