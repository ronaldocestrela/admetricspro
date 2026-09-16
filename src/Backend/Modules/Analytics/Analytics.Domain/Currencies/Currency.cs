using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Currencies;

/// <summary>
/// Representa uma moeda no padrão internacional ISO 4217, garantindo normalização e integridade cadastral.
/// </summary>
public sealed record Currency
{
    /// <summary>
    /// Código ISO da moeda Real Brasileiro.
    /// </summary>
    public const string BRL = "BRL";

    /// <summary>
    /// Código ISO da moeda Dólar Americano.
    /// </summary>
    public const string USD = "USD";

    /// <summary>
    /// Código ISO da moeda Euro.
    /// </summary>
    public const string EUR = "EUR";

    /// <summary>
    /// Código ISO da moeda Libra Esterlina.
    /// </summary>
    public const string GBP = "GBP";

    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        BRL, USD, EUR, GBP
    };

    /// <summary>
    /// Obtém o código ISO de 3 letras normalizado em caixa alta.
    /// </summary>
    public string Code { get; }

    private Currency(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Cria e valida uma moeda ISO 4217.
    /// </summary>
    /// <param name="code">Código ISO de 3 letras da moeda.</param>
    /// <returns>Resultado contendo a moeda validada ou erro de validação.</returns>
    public static Result<Currency> Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<Currency>.Failure(
                Error.Validation("Currency.EmptyCode", "O código da moeda não pode ser nulo ou vazio."));
        }

        var normalized = code.Trim().ToUpperInvariant();

        if (normalized.Length != 3)
        {
            return Result<Currency>.Failure(
                Error.Validation("Currency.InvalidLength", $"O código de moeda '{code}' deve possuir exatamente 3 caracteres."));
        }

        if (!SupportedCurrencies.Contains(normalized))
        {
            return Result<Currency>.Failure(
                Error.Validation("Currency.Unsupported", $"A moeda '{normalized}' não é suportada atualmente. Moedas suportadas: BRL, USD, EUR, GBP."));
        }

        return Result<Currency>.Success(new Currency(normalized));
    }

    /// <summary>
    /// Valida se uma string é um código de moeda suportado.
    /// </summary>
    /// <param name="code">Código a ser verificado.</param>
    /// <returns>Verdadeiro se suportado; falso caso contrário.</returns>
    public static bool IsSupported(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return SupportedCurrencies.Contains(code.Trim().ToUpperInvariant());
    }

    /// <inheritdoc />
    public override string ToString() => Code;
}
