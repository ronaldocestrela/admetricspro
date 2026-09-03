namespace BuildingBlocks.Application.Security;

/// <summary>
/// Defines data sanitization and masking operations to protect sensitive financial and billing information during impersonation.
/// </summary>
public interface IBillingDataMasker
{
    /// <summary>
    /// Masks a credit card number, preserving only the last 4 digits (e.g. "**** **** **** 1234").
    /// </summary>
    /// <param name="cardNumber">Raw credit card number.</param>
    /// <returns>Sanitized credit card string, or null if input was null.</returns>
    string? MaskCreditCard(string? cardNumber);

    /// <summary>
    /// Masks a tax/fiscal document (CPF or CNPJ), obfuscating intermediate identification digits.
    /// </summary>
    /// <param name="document">Raw document digits or formatted string.</param>
    /// <returns>Masked tax document string, or null if input was null.</returns>
    string? MaskTaxDocument(string? document);

    /// <summary>
    /// Masks bank account details (agency, account number, PIX keys) for privacy protection.
    /// </summary>
    /// <param name="bankDetails">Raw bank details string.</param>
    /// <returns>Masked bank details string, or null if input was null.</returns>
    string? MaskBankDetails(string? bankDetails);

    /// <summary>
    /// Sanitizes an input string based on the current impersonation context.
    /// If impersonation is active, returns the masked value; otherwise returns the original value.
    /// </summary>
    /// <param name="sensitiveValue">Sensitive value to evaluate.</param>
    /// <param name="maskFunc">Masking transformation function to apply if impersonation is active.</param>
    /// <returns>Sanitized or original string.</returns>
    string? SanitizeIfImpersonated(string? sensitiveValue, Func<string?, string?> maskFunc);
}
