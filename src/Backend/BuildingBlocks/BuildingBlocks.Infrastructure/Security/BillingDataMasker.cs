using System.Text.RegularExpressions;
using BuildingBlocks.Application.Security;

namespace BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Implements <see cref="IBillingDataMasker"/> to sanitize financial and sensitive data
/// when operating under tenant impersonation (Shadow Mode).
/// </summary>
public sealed class BillingDataMasker : IBillingDataMasker
{
    private readonly IImpersonationContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingDataMasker"/> class.
    /// </summary>
    /// <param name="contextAccessor">Accessor for evaluating active impersonation state.</param>
    public BillingDataMasker(IImpersonationContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public string? MaskCreditCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return null;
        }

        var digits = Regex.Replace(cardNumber, @"\D", "");
        if (digits.Length < 4)
        {
            return "**** **** **** ****";
        }

        var lastFour = digits[^4..];
        return $"**** **** **** {lastFour}";
    }

    /// <inheritdoc />
    public string? MaskTaxDocument(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            return null;
        }

        var digits = Regex.Replace(document, @"\D", "");

        // CPF (11 digits): ***.***.789-**
        if (digits.Length == 11)
        {
            var part = digits[6..9];
            return $"***.***.{part}-**";
        }

        // CNPJ (14 digits): **.***.333/****-81
        if (digits.Length == 14)
        {
            var middle = digits[5..8];
            var end = digits[12..14];
            return $"**.***.{middle}/****-{end}";
        }

        // Generic fallback masking
        if (digits.Length > 4)
        {
            var suffix = digits[^2..];
            return $"***.***.{suffix}";
        }

        return "***";
    }

    /// <inheritdoc />
    public string? MaskBankDetails(string? bankDetails)
    {
        if (string.IsNullOrWhiteSpace(bankDetails))
        {
            return null;
        }

        // Obfuscate digit sequences greater than 3 digits
        return Regex.Replace(bankDetails, @"\d{4,}", m => new string('*', m.Length - 1) + m.Value[^1..]);
    }

    /// <inheritdoc />
    public string? SanitizeIfImpersonated(string? sensitiveValue, Func<string?, string?> maskFunc)
    {
        ArgumentNullException.ThrowIfNull(maskFunc);

        if (sensitiveValue is null)
        {
            return null;
        }

        if (_contextAccessor.Current.IsImpersonated)
        {
            return maskFunc(sensitiveValue);
        }

        return sensitiveValue;
    }
}
