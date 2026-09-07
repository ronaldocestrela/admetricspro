using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o validador algorítmico de documentos fiscais brasileiros (<see cref="TaxDocumentValidator"/>).
/// </summary>
public sealed class TaxDocumentValidatorTests
{
    /// <summary>
    /// Valida que CPFs válidos e inválidos são discriminados com precisão segundo os dígitos verificadores.
    /// </summary>
    /// <param name="cpf">Número de CPF sob análise.</param>
    /// <param name="expected">Resultado esperado da validação.</param>
    [Theory]
    [InlineData("52998224725", true)] // CPF válido
    [InlineData("00000000191", true)] // CPF válido
    [InlineData("11144477735", true)] // CPF válido
    [InlineData("529.982.247-25", true)] // CPF formatado válido
    [InlineData("11111111111", false)] // CPF repetido
    [InlineData("00000000000", false)] // CPF repetido
    [InlineData("12345678901", false)] // CPF com dígito verificador inválido
    [InlineData("123", false)] // Menos de 11 dígitos
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValidCpf_ShouldValidateCorrectly(string? cpf, bool expected)
    {
        var result = TaxDocumentValidator.IsValidCpf(cpf);
        result.Should().Be(expected);
    }

    /// <summary>
    /// Valida que CNPJs válidos e inválidos são discriminados com precisão segundo os dígitos verificadores.
    /// </summary>
    /// <param name="cnpj">Número de CNPJ sob análise.</param>
    /// <param name="expected">Resultado esperado da validação.</param>
    [Theory]
    [InlineData("12345678000195", true)] // CNPJ válido
    [InlineData("11222333000181", true)] // CNPJ válido
    [InlineData("00000000000191", true)] // CNPJ válido
    [InlineData("12.345.678/0001-95", true)] // CNPJ formatado válido
    [InlineData("11111111111111", false)] // CNPJ repetido
    [InlineData("00000000000000", false)] // CNPJ repetido
    [InlineData("12345678000100", false)] // CNPJ com dígito verificador inválido
    [InlineData("1234567890", false)] // Menos de 14 dígitos
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValidCnpj_ShouldValidateCorrectly(string? cnpj, bool expected)
    {
        var result = TaxDocumentValidator.IsValidCnpj(cnpj);
        result.Should().Be(expected);
    }

    /// <summary>
    /// Valida que o método genérico IsValid atende tanto a CPF quanto a CNPJ.
    /// </summary>
    /// <param name="document">Documento a ser validado.</param>
    /// <param name="expected">Resultado esperado.</param>
    [Theory]
    [InlineData("52998224725", true)] // CPF válido
    [InlineData("12345678000195", true)] // CNPJ válido
    [InlineData("529.982.247-25", true)] // CPF formatado válido
    [InlineData("12.345.678/0001-95", true)] // CNPJ formatado válido
    [InlineData("12345678901", false)] // CPF inválido
    [InlineData("12345678000100", false)] // CNPJ inválido
    [InlineData("12345", false)] // Comprimento inválido
    public void IsValid_ShouldValidateBothCpfAndCnpj(string document, bool expected)
    {
        var result = TaxDocumentValidator.IsValid(document);
        result.Should().Be(expected);
    }

    /// <summary>
    /// Valida que IsValidFormat valida apenas o comprimento numérico de 11 ou 14 dígitos.
    /// </summary>
    /// <param name="document">Documento a ser validado.</param>
    /// <param name="expected">Resultado esperado.</param>
    [Theory]
    [InlineData("52998224725", true)] // 11 dígitos
    [InlineData("12345678000195", true)] // 14 dígitos
    [InlineData("529.982.247-25", true)] // 11 dígitos com máscara
    [InlineData("12.345.678/0001-95", true)] // 14 dígitos com máscara
    [InlineData("1234567890", false)] // 10 dígitos
    [InlineData("123456789012", false)] // 12 dígitos
    [InlineData("123456789012345", false)] // 15 dígitos
    public void IsValidFormat_ShouldValidateLengthOnly(string document, bool expected)
    {
        var result = TaxDocumentValidator.IsValidFormat(document);
        result.Should().Be(expected);
    }

    /// <summary>
    /// Valida que a formatação aplica a máscara de pontuação apropriada para CPF e CNPJ.
    /// </summary>
    /// <param name="raw">Documento sanitizado bruto.</param>
    /// <param name="expectedFormatted">Formatação esperada.</param>
    [Theory]
    [InlineData("52998224725", "529.982.247-25")]
    [InlineData("12345678000195", "12.345.678/0001-95")]
    public void Format_ShouldApplyCorrectMask(string raw, string expectedFormatted)
    {
        var formatted = TaxDocumentValidator.Format(raw);
        formatted.Should().Be(expectedFormatted);
    }
}
