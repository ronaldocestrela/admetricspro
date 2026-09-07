using System.Text.RegularExpressions;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Validador e formatador utilitário para documentos fiscais brasileiros (CPF e CNPJ).
/// Executa a validação matemática oficial dos dígitos verificadores (módulo 11) e sanitização de entradas.
/// </summary>
public static partial class TaxDocumentValidator
{
    private static readonly int[] CpfMultipliers1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CpfMultipliers2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

    private static readonly int[] CnpjMultipliers1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CnpjMultipliers2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Sanitiza o documento informado, removendo todos os caracteres não numéricos.
    /// </summary>
    /// <param name="document">Documento fiscal bruto ou formatado com máscara.</param>
    /// <returns>Cadeia contendo apenas dígitos numéricos.</returns>
    public static string Sanitize(string? document)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            return string.Empty;
        }

        return NonDigitsRegex().Replace(document, string.Empty);
    }

    /// <summary>
    /// Verifica se o documento sanitizado possui comprimento válido para CPF (11 dígitos) ou CNPJ (14 dígitos).
    /// </summary>
    /// <param name="document">Documento a ser analisado.</param>
    /// <returns><c>true</c> se contiver exatamente 11 ou 14 dígitos numéricos; caso contrário, <c>false</c>.</returns>
    public static bool IsValidFormat(string? document)
    {
        var sanitized = Sanitize(document);
        return sanitized.Length == 11 || sanitized.Length == 14;
    }

    /// <summary>
    /// Valida um CPF (Cadastro de Pessoas Físicas) verificando comprimento, repetições inválidas e dígitos verificadores.
    /// </summary>
    /// <param name="cpf">Número de CPF com ou sem pontuação.</param>
    /// <returns><c>true</c> se o CPF for válido segundo o algoritmo oficial; caso contrário, <c>false</c>.</returns>
    public static bool IsValidCpf(string? cpf)
    {
        var sanitized = Sanitize(cpf);

        if (sanitized.Length != 11)
        {
            return false;
        }

        // Rejeita sequências com todos os dígitos iguais (ex: 000.000.000-00, 111.111.111-11)
        if (AreAllDigitsEqual(sanitized))
        {
            return false;
        }

        var tempCpf = sanitized[..9];
        var sum = 0;

        for (var i = 0; i < 9; i++)
        {
            sum += (tempCpf[i] - '0') * CpfMultipliers1[i];
        }

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (sanitized[9] - '0' != digit1)
        {
            return false;
        }

        tempCpf += digit1;
        sum = 0;

        for (var i = 0; i < 10; i++)
        {
            sum += (tempCpf[i] - '0') * CpfMultipliers2[i];
        }

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return sanitized[10] - '0' == digit2;
    }

    /// <summary>
    /// Valida um CNPJ (Cadastro Nacional da Pessoa Jurídica) verificando comprimento, repetições inválidas e dígitos verificadores.
    /// </summary>
    /// <param name="cnpj">Número de CNPJ com ou sem pontuação.</param>
    /// <returns><c>true</c> se o CNPJ for válido segundo o algoritmo oficial; caso contrário, <c>false</c>.</returns>
    public static bool IsValidCnpj(string? cnpj)
    {
        var sanitized = Sanitize(cnpj);

        if (sanitized.Length != 14)
        {
            return false;
        }

        // Rejeita sequências com todos os dígitos iguais (ex: 00.000.000/0000-00, 11.111.111/1111-11)
        if (AreAllDigitsEqual(sanitized))
        {
            return false;
        }

        var tempCnpj = sanitized[..12];
        var sum = 0;

        for (var i = 0; i < 12; i++)
        {
            sum += (tempCnpj[i] - '0') * CnpjMultipliers1[i];
        }

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (sanitized[12] - '0' != digit1)
        {
            return false;
        }

        tempCnpj += digit1;
        sum = 0;

        for (var i = 0; i < 13; i++)
        {
            sum += (tempCnpj[i] - '0') * CnpjMultipliers2[i];
        }

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return sanitized[13] - '0' == digit2;
    }

    /// <summary>
    /// Valida um documento fiscal genérico (CPF ou CNPJ) aplicando o algoritmo correspondente com base no comprimento.
    /// </summary>
    /// <param name="document">Documento a ser validado.</param>
    /// <returns><c>true</c> se for um CPF válido ou CNPJ válido; caso contrário, <c>false</c>.</returns>
    public static bool IsValid(string? document)
    {
        var sanitized = Sanitize(document);

        return sanitized.Length switch
        {
            11 => IsValidCpf(sanitized),
            14 => IsValidCnpj(sanitized),
            _ => false
        };
    }

    /// <summary>
    /// Formata um documento sanitizado com a máscara correspondente (CPF ou CNPJ).
    /// </summary>
    /// <param name="document">Documento a ser formatado.</param>
    /// <returns>Cadeia formatada com pontuação ou original caso não atenda aos tamanhos conhecidos.</returns>
    public static string Format(string? document)
    {
        var sanitized = Sanitize(document);

        if (sanitized.Length == 11)
        {
            return Convert.ToUInt64(sanitized).ToString(@"000\.000\.000\-00");
        }

        if (sanitized.Length == 14)
        {
            return Convert.ToUInt64(sanitized).ToString(@"00\.000\.000\/0000\-00");
        }

        return document ?? string.Empty;
    }

    private static bool AreAllDigitsEqual(string input)
    {
        var first = input[0];
        for (var i = 1; i < input.Length; i++)
        {
            if (input[i] != first)
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigitsRegex();
}
