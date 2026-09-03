using FluentAssertions;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;

namespace UnitTests.Backend.Dunning;

/// <summary>
/// Unit tests for <see cref="Tenant"/> dunning lifecycle and domain event publishing.
/// </summary>
public sealed class TenantDunningTests
{
    private readonly DateTime _referenceUtc = new(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Verifies that a newly created tenant starts with DunningStage.None and no payment due date.
    /// </summary>
    [Fact]
    public void Create_ShouldInitializeWithDunningStageNoneAndNullDueDate()
    {
        // Act
        var result = Tenant.Create("Agencia Nova", "12345678000190", "agencia-nova");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenant = result.Value;
        tenant.DunningStage.Should().Be(DunningStage.None);
        tenant.PaymentDueDateUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies marking payment overdue sets the due date.
    /// </summary>
    [Fact]
    public void MarkPaymentOverdue_ShouldSetPaymentDueDate()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Teste", "12345678000190", "agencia-teste").Value;
        var dueDate = _referenceUtc.AddDays(-2);

        // Act
        var result = tenant.MarkPaymentOverdue(dueDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.PaymentDueDateUtc.Should().Be(dueDate);
    }

    /// <summary>
    /// Verifies that evaluating dunning within grace period (e.g. D+2) keeps stage None and does not raise event.
    /// </summary>
    [Fact]
    public void EvaluateDunningStage_ShouldKeepStageNone_DuringGracePeriod()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Grace", "12345678000190", "agencia-grace").Value;
        tenant.MarkPaymentOverdue(_referenceUtc.AddDays(-2));

        // Act
        var result = tenant.EvaluateDunningStage(_referenceUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.DunningStage.Should().Be(DunningStage.None);
        tenant.DomainEvents.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that evaluating dunning at D+4 transitions stage to AutomationsDisabled and raises TenantGracePeriodExceededEvent.
    /// </summary>
    [Fact]
    public void EvaluateDunningStage_ShouldTransitionToAutomationsDisabledAndRaiseEvent_WhenDPlus4()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia D4", "12345678000190", "agencia-d4").Value;
        var dueDate = _referenceUtc.AddDays(-4);
        tenant.MarkPaymentOverdue(dueDate);

        // Act
        var result = tenant.EvaluateDunningStage(_referenceUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.DunningStage.Should().Be(DunningStage.AutomationsDisabled);
        tenant.Status.Should().Be(TenantStatus.Active);

        tenant.DomainEvents.Should().ContainSingle();
        var domainEvent = tenant.DomainEvents.First().Should().BeOfType<TenantGracePeriodExceededEvent>().Subject;
        domainEvent.TenantId.Should().Be(tenant.Id);
        domainEvent.PreviousStage.Should().Be(DunningStage.None);
        domainEvent.CurrentStage.Should().Be(DunningStage.AutomationsDisabled);
        domainEvent.DaysOverdue.Should().Be(4);
    }

    /// <summary>
    /// Verifies that evaluating dunning at D+8 transitions stage to ReportsBlocked and raises event.
    /// </summary>
    [Fact]
    public void EvaluateDunningStage_ShouldTransitionToReportsBlockedAndRaiseEvent_WhenDPlus8()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia D8", "12345678000190", "agencia-d8").Value;
        var dueDate = _referenceUtc.AddDays(-8);
        tenant.MarkPaymentOverdue(dueDate);

        // Act
        var result = tenant.EvaluateDunningStage(_referenceUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.DunningStage.Should().Be(DunningStage.ReportsBlocked);
        tenant.Status.Should().Be(TenantStatus.Active);

        var domainEvent = tenant.DomainEvents.OfType<TenantGracePeriodExceededEvent>().Single();
        domainEvent.CurrentStage.Should().Be(DunningStage.ReportsBlocked);
        domainEvent.DaysOverdue.Should().Be(8);
    }

    /// <summary>
    /// Verifies that evaluating dunning at D+15 transitions stage to LoginBlocked, changes Status to Suspended, and raises event.
    /// </summary>
    [Fact]
    public void EvaluateDunningStage_ShouldSuspendTenantAndBlockLogin_WhenDPlus15()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia D15", "12345678000190", "agencia-d15").Value;
        var dueDate = _referenceUtc.AddDays(-15);
        tenant.MarkPaymentOverdue(dueDate);

        // Act
        var result = tenant.EvaluateDunningStage(_referenceUtc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.DunningStage.Should().Be(DunningStage.LoginBlocked);
        tenant.Status.Should().Be(TenantStatus.Suspended);

        var domainEvent = tenant.DomainEvents.OfType<TenantGracePeriodExceededEvent>().Single();
        domainEvent.CurrentStage.Should().Be(DunningStage.LoginBlocked);
        domainEvent.DaysOverdue.Should().Be(15);
    }

    /// <summary>
    /// Verifies that regularizing payment resets dunning stage to None, clears due date, and restores Active status if suspended.
    /// </summary>
    [Fact]
    public void RegularizePayment_ShouldResetDunningStageAndRestoreActiveStatus()
    {
        // Arrange
        var tenant = Tenant.Create("Agencia Regular", "12345678000190", "agencia-regular").Value;
        tenant.MarkPaymentOverdue(_referenceUtc.AddDays(-20));
        tenant.EvaluateDunningStage(_referenceUtc);
        tenant.DunningStage.Should().Be(DunningStage.LoginBlocked);
        tenant.Status.Should().Be(TenantStatus.Suspended);

        // Act
        var result = tenant.RegularizePayment();

        // Assert
        result.IsSuccess.Should().BeTrue();
        tenant.DunningStage.Should().Be(DunningStage.None);
        tenant.PaymentDueDateUtc.Should().BeNull();
        tenant.Status.Should().Be(TenantStatus.Active);
    }
}
