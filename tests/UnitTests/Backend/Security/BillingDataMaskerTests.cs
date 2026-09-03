using System.Security.Claims;
using BuildingBlocks.Application.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace UnitTests.Backend.Security;

/// <summary>
/// Unit tests for <see cref="IBillingDataMasker"/> and impersonation context sensitivity.
/// </summary>
public sealed class BillingDataMaskerTests
{
    private readonly IImpersonationContextAccessor _contextAccessor = Substitute.For<IImpersonationContextAccessor>();

    private BuildingBlocks.Infrastructure.Security.BillingDataMasker CreateMasker(bool isImpersonated = true)
    {
        var context = Substitute.For<IImpersonationContext>();
        context.IsImpersonated.Returns(isImpersonated);
        _contextAccessor.Current.Returns(context);

        return new BuildingBlocks.Infrastructure.Security.BillingDataMasker(_contextAccessor);
    }

    /// <summary>
    /// Verifies that credit card numbers are masked preserving only the last four digits.
    /// </summary>
    [Theory]
    [InlineData("4111222233334444", "**** **** **** 4444")]
    [InlineData("4111 2222 3333 4444", "**** **** **** 4444")]
    [InlineData("5500-0000-0000-1234", "**** **** **** 1234")]
    [InlineData("1234", "**** **** **** 1234")]
    public void MaskCreditCard_ShouldPreserveOnlyLastFourDigits(string input, string expected)
    {
        // Arrange
        var masker = CreateMasker();

        // Act
        var result = masker.MaskCreditCard(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that MaskCreditCard returns null or empty when input is null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MaskCreditCard_ShouldReturnNullOrEmpty_WhenInputIsNullOrWhitespace(string? input)
    {
        // Arrange
        var masker = CreateMasker();

        // Act
        var result = masker.MaskCreditCard(input);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that CPF tax documents are masked with standard obfuscation format.
    /// </summary>
    [Theory]
    [InlineData("12345678900", "***.***.789-**")]
    [InlineData("123.456.789-00", "***.***.789-**")]
    public void MaskTaxDocument_ShouldMaskCpfCorrectly(string input, string expected)
    {
        // Arrange
        var masker = CreateMasker();

        // Act
        var result = masker.MaskTaxDocument(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that CNPJ tax documents are masked with standard corporate obfuscation format.
    /// </summary>
    [Theory]
    [InlineData("11222333000181", "**.***.333/****-81")]
    [InlineData("11.222.333/0001-81", "**.***.333/****-81")]
    public void MaskTaxDocument_ShouldMaskCnpjCorrectly(string input, string expected)
    {
        // Arrange
        var masker = CreateMasker();

        // Act
        var result = masker.MaskTaxDocument(input);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that Bank details are obfuscated for privacy.
    /// </summary>
    [Fact]
    public void MaskBankDetails_ShouldObfuscateAccountData()
    {
        // Arrange
        var masker = CreateMasker();
        var rawDetails = "Banco do Brasil - Ag: 1234-5 / CC: 9876543-2";

        // Act
        var result = masker.MaskBankDetails(rawDetails);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotContain("9876543");
        result.Should().Contain("***");
    }

    /// <summary>
    /// Verifies that SanitizeIfImpersonated returns masked value when impersonation is active.
    /// </summary>
    [Fact]
    public void SanitizeIfImpersonated_ShouldApplyMask_WhenImpersonationIsActive()
    {
        // Arrange
        var masker = CreateMasker(isImpersonated: true);
        const string rawCard = "4111222233334444";

        // Act
        var result = masker.SanitizeIfImpersonated(rawCard, masker.MaskCreditCard);

        // Assert
        result.Should().Be("**** **** **** 4444");
    }

    /// <summary>
    /// Verifies that SanitizeIfImpersonated returns original value when impersonation is not active.
    /// </summary>
    [Fact]
    public void SanitizeIfImpersonated_ShouldKeepOriginalValue_WhenImpersonationIsNotActive()
    {
        // Arrange
        var masker = CreateMasker(isImpersonated: false);
        const string rawCard = "4111222233334444";

        // Act
        var result = masker.SanitizeIfImpersonated(rawCard, masker.MaskCreditCard);

        // Assert
        result.Should().Be(rawCard);
    }
}
