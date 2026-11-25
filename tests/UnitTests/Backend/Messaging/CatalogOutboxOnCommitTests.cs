using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Messaging.Persistence;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Billing.Checkout.Commands.ProcessCheckout;
using Master.Application.Billing.Payments;
using Master.Application.Messaging;
using Master.Application.Repositories;
using Master.Domain.Billing;
using Master.Domain.Plans;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Messaging;

/// <summary>
/// Garante gravação de outbox de catálogo no mesmo commit quando <see cref="IMessagingFeatureGate"/> está ativo.
/// </summary>
public sealed class CatalogOutboxOnCommitTests
{
  private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();
  private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
  private readonly ITenantPaymentTransactionRepository _transactionRepository = Substitute.For<ITenantPaymentTransactionRepository>();
  private readonly IPaymentGatewayService _paymentGateway = Substitute.For<IPaymentGatewayService>();
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly ICatalogIntegrationOutboxWriter _catalogOutbox = Substitute.For<ICatalogIntegrationOutboxWriter>();
  private readonly IMessagingFeatureGate _messagingGate = Substitute.For<IMessagingFeatureGate>();

  private readonly SubscriptionPlan _plan = SubscriptionPlan.Create(
    "Pro",
    "Pro",
    SubscriptionTier.Pro,
    497m,
    20,
    PlanLimits.Create(5, 5, 50_000m).Value,
    PlanFeatures.Default()).Value;

  /// <summary>
  /// Checkout aprovado com mensageria ligada grava outbox de confirmação e não bloqueia no SMTP.
  /// </summary>
  [Fact]
  public async Task ProcessCheckout_WhenMessagingEnabled_EnqueuesSubscriptionConfirmationOutbox()
  {
    _messagingGate.IsEnabled.Returns(true);
    _catalogOutbox.AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    var tenant = Tenant.Create("Agência", "12345678000190", "ag", SubscriptionTier.Trial, adminEmail: "owner@ag.com").Value;
    _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
    _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>()).Returns(_plan);
    _paymentGateway.GetOrCreateCustomerAsync(Arg.Any<PaymentCustomerRequest>(), Arg.Any<CancellationToken>())
      .Returns(new PaymentGatewayCustomer("cus", "test"));
    _paymentGateway.ChargeCreditCardAsync(Arg.Any<CreditCardChargeRequest>(), Arg.Any<CancellationToken>())
      .Returns(new CreditCardChargeResult("tx", IsSuccess: true));

    var handler = new ProcessCheckoutCommandHandler(
      _tenantRepository,
      _planRepository,
      _transactionRepository,
      _paymentGateway,
      _unitOfWork,
      _catalogOutbox,
      _messagingGate);

    var result = await handler.Handle(
      new ProcessCheckoutCommand(
        tenant.Id.Value,
        SubscriptionTier.Pro,
        "Monthly",
        PaymentMethod.CreditCard,
        CardHolderName: "Owner",
        CardNumber: "4111111111111111",
        ExpiryMonth: "12",
        ExpiryYear: "2030",
        Ccv: "123"),
      CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    await _catalogOutbox.Received(1).AddAsync(
      Arg.Is<IntegrationOutboxMessage>(m => m.MessageType == CatalogIntegrationMessageTypes.SubscriptionConfirmationEmail),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
  }

  /// <summary>
  /// Com mensageria desligada o checkout não grava outbox.
  /// </summary>
  [Fact]
  public async Task ProcessCheckout_WhenMessagingDisabled_DoesNotEnqueueOutbox()
  {
    _messagingGate.IsEnabled.Returns(false);

    var tenant = Tenant.Create("Agência", "12345678000190", "ag", SubscriptionTier.Trial, adminEmail: "owner@ag.com").Value;
    _tenantRepository.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
    _planRepository.GetByTierAsync(SubscriptionTier.Pro, Arg.Any<CancellationToken>()).Returns(_plan);
    _paymentGateway.GetOrCreateCustomerAsync(Arg.Any<PaymentCustomerRequest>(), Arg.Any<CancellationToken>())
      .Returns(new PaymentGatewayCustomer("cus", "test"));
    _paymentGateway.ChargeCreditCardAsync(Arg.Any<CreditCardChargeRequest>(), Arg.Any<CancellationToken>())
      .Returns(new CreditCardChargeResult("tx", IsSuccess: true));

    var handler = new ProcessCheckoutCommandHandler(
      _tenantRepository,
      _planRepository,
      _transactionRepository,
      _paymentGateway,
      _unitOfWork,
      _catalogOutbox,
      _messagingGate);

    var result = await handler.Handle(
      new ProcessCheckoutCommand(
        tenant.Id.Value,
        SubscriptionTier.Pro,
        "Monthly",
        PaymentMethod.CreditCard,
        CardHolderName: "Owner",
        CardNumber: "4111111111111111",
        ExpiryMonth: "12",
        ExpiryYear: "2030",
        Ccv: "123"),
      CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    await _catalogOutbox.DidNotReceive().AddAsync(Arg.Any<IntegrationOutboxMessage>(), Arg.Any<CancellationToken>());
  }

  /// <summary>
  /// Factory de boas-vindas cria tipo de mensagem esperado.
  /// </summary>
  [Fact]
  public void WelcomeOutboxFactory_ProducesExpectedMessageType()
  {
    var tenantId = Guid.NewGuid();
    var line = CatalogIntegrationOutboxFactory.CreateWelcomeEmailOutbox(tenantId, tenantId, DateTime.UtcNow);
    line.IsSuccess.Should().BeTrue();
    line.Value.MessageType.Should().Be(CatalogIntegrationMessageTypes.TenantWelcomeEmail);
    line.Value.TenantId.Should().Be(tenantId);
  }
}