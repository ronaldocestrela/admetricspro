using FluentAssertions;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para ativação definitiva de assinatura paga e transição de estado no agregado <see cref="Tenant"/>.
/// </summary>
public sealed class TenantSubscriptionActivationTests
{
    /// <summary>
    /// Valida que a ativação definitiva com ciclo mensal atualiza o status para Active, o tier correto,
    /// a data de expiração para +30 dias e dispara o evento de domínio <see cref="TenantSubscriptionActivatedDomainEvent"/>.
    /// </summary>
    [Fact]
    public void ActivatePaidSubscription_WithMonthlyCycle_ShouldUpdateStateAndRaiseDomainEvent()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Agência Performance",
            "12345678000199",
            "performance",
            SubscriptionTier.Trial,
            adminEmail: "contato@performance.com.br",
            adminFullName: "Carlos Silva").Value;

        var referenceUtc = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc);
        var expectedExpiresAt = referenceUtc.AddDays(30);

        // Act
        var result = tenant.ActivatePaidSubscription(
            SubscriptionTier.Pro,
            "Monthly",
            referenceUtc,
            497.00m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.Tier.Should().Be(SubscriptionTier.Pro);
        tenant.BillingCycle.Should().Be("Monthly");
        tenant.SubscriptionExpiresAtUtc.Should().Be(expectedExpiresAt);
        tenant.DunningStage.Should().Be(DunningStage.None);
        tenant.PaymentDueDateUtc.Should().BeNull();

        // Validação do evento de domínio
        tenant.DomainEvents.Should().ContainSingle(e => e is TenantSubscriptionActivatedDomainEvent);
        var domainEvent = (TenantSubscriptionActivatedDomainEvent)tenant.DomainEvents.Single(e => e is TenantSubscriptionActivatedDomainEvent);
        domainEvent.TenantId.Should().Be(tenant.Id);
        domainEvent.CompanyName.Should().Be("Agência Performance");
        domainEvent.AdminEmail.Should().Be("contato@performance.com.br");
        domainEvent.Tier.Should().Be(SubscriptionTier.Pro);
        domainEvent.BillingCycle.Should().Be("Monthly");
        domainEvent.Amount.Should().Be(497.00m);
        domainEvent.PaidAtUtc.Should().Be(referenceUtc);
        domainEvent.ExpiresAtUtc.Should().Be(expectedExpiresAt);
    }

    /// <summary>
    /// Valida que a ativação definitiva com ciclo anual define expiração para +365 dias.
    /// </summary>
    [Fact]
    public void ActivatePaidSubscription_WithAnnualCycle_ShouldSetOneYearExpiration()
    {
        // Arrange
        var tenant = Tenant.Create(
            "Agência Anual",
            "98765432000188",
            "agencia-anual",
            SubscriptionTier.Trial).Value;

        var referenceUtc = new DateTime(2026, 9, 10, 10, 0, 0, DateTimeKind.Utc);
        var expectedExpiresAt = referenceUtc.AddDays(365);

        // Act
        var result = tenant.ActivatePaidSubscription(
            SubscriptionTier.Starter,
            "Annual",
            referenceUtc,
            1891.20m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.Tier.Should().Be(SubscriptionTier.Starter);
        tenant.BillingCycle.Should().Be("Annual");
        tenant.SubscriptionExpiresAtUtc.Should().Be(expectedExpiresAt);
    }

    /// <summary>
    /// Valida que tentar ativar o plano com Trial resulta em falha de validação.
    /// </summary>
    [Fact]
    public void ActivatePaidSubscription_WithTrialTier_ShouldFail()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Trial", "12345678000199", "trial-agency").Value;

        // Act
        var result = tenant.ActivatePaidSubscription(
            SubscriptionTier.Trial,
            "Monthly",
            DateTime.UtcNow,
            0m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidPaidTier");
    }

    /// <summary>
    /// Valida que ciclo de cobrança inválido resulta em erro.
    /// </summary>
    [Fact]
    public void ActivatePaidSubscription_WithInvalidCycle_ShouldFail()
    {
        // Arrange
        var tenant = Tenant.Create("Agência Ciclo", "12345678000199", "ciclo-agency").Value;

        // Act
        var result = tenant.ActivatePaidSubscription(
            SubscriptionTier.Pro,
            "Semestral",
            DateTime.UtcNow,
            400m);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.InvalidBillingCycle");
    }
}
