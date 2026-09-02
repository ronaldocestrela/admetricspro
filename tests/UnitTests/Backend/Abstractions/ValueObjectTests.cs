using BuildingBlocks.Domain.Abstractions;
using FluentAssertions;

namespace UnitTests.Backend.Abstractions;

/// <summary>
/// Unit tests for <see cref="ValueObject"/>.
/// </summary>
public sealed class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public Money(decimal amount, string? currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public decimal Amount { get; }
        public string? Currency { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    private sealed class AnotherValueObject : ValueObject
    {
        public AnotherValueObject(decimal amount)
        {
            Amount = amount;
        }

        public decimal Amount { get; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
        }
    }

    /// <summary>
    /// Verifies equality when all components match.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnTrue_WhenComponentsAreEqual()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(100.50m, "USD");

        // Act & Assert
        money1.Equals(money2).Should().BeTrue();
        (money1 == money2).Should().BeTrue();
        (money1 != money2).Should().BeFalse();
    }

    /// <summary>
    /// Verifies inequality when components differ.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComponentsDiffer()
    {
        // Arrange
        var money1 = new Money(100.50m, "USD");
        var money2 = new Money(200.00m, "USD");
        var money3 = new Money(100.50m, "BRL");

        // Act & Assert
        money1.Equals(money2).Should().BeFalse();
        (money1 == money2).Should().BeFalse();
        (money1 != money2).Should().BeTrue();

        money1.Equals(money3).Should().BeFalse();
        (money1 == money3).Should().BeFalse();
        (money1 != money3).Should().BeTrue();
    }

    /// <summary>
    /// Verifies inequality when compared with null.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithNull()
    {
        // Arrange
        var money = new Money(50.0m, "EUR");

        // Act & Assert
        money.Equals(null).Should().BeFalse();
        (money == null).Should().BeFalse();
        (null == money).Should().BeFalse();
        (money != null).Should().BeTrue();
        (null != money).Should().BeTrue();
    }

    /// <summary>
    /// Verifies equality when comparing two null references.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingNullWithNull()
    {
        Money? nullMoney1 = null;
        Money? nullMoney2 = null;

        (nullMoney1 == nullMoney2).Should().BeTrue();
        (nullMoney1 != nullMoney2).Should().BeFalse();
    }

    /// <summary>
    /// Verifies inequality when comparing different ValueObject derived types.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingDifferentValueObjectTypes()
    {
        // Arrange
        var money = new Money(100m, "USD");
        var another = new AnotherValueObject(100m);

        // Act & Assert
        money.Equals(another).Should().BeFalse();
    }

    /// <summary>
    /// Verifies null equality components do not cause NullReferenceException.
    /// </summary>
    [Fact]
    public void Equals_ShouldHandleNullComponentsWithoutThrowing()
    {
        // Arrange
        var moneyWithNull1 = new Money(100m, null);
        var moneyWithNull2 = new Money(100m, null);
        var moneyWithCurrency = new Money(100m, "USD");

        // Act & Assert
        moneyWithNull1.Equals(moneyWithNull2).Should().BeTrue();
        (moneyWithNull1 == moneyWithNull2).Should().BeTrue();

        moneyWithNull1.Equals(moneyWithCurrency).Should().BeFalse();
        (moneyWithNull1 == moneyWithCurrency).Should().BeFalse();
    }

    /// <summary>
    /// Verifies GetHashCode returns identical hash when components match.
    /// </summary>
    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenComponentsAreEqual()
    {
        // Arrange
        var money1 = new Money(150.75m, "USD");
        var money2 = new Money(150.75m, "USD");

        // Act & Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    /// <summary>
    /// Verifies GetHashCode handles null components gracefully.
    /// </summary>
    [Fact]
    public void GetHashCode_ShouldNotThrow_WhenComponentsContainNull()
    {
        // Arrange
        var money = new Money(150.75m, null);

        // Act
        var act = () => money.GetHashCode();

        // Assert
        act.Should().NotThrow();
    }
}
