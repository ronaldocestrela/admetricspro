using System.Text.Json;
using FluentAssertions;
using Integrations.Application.Meta;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Validações de escrita não podem habilitar gasto por padrão.</summary>
public sealed class MetaWriteValidationTests
{
    /// <summary>Novos recursos publicitários são criados pausados.</summary>
    [Fact]
    public void Validate_NewCampaign_DefaultsToPaused()
    {
        var result = MetaWriteValidation.Validate("campaigns", null,
            JsonSerializer.SerializeToElement(new { name = "Nova", objective = "OUTCOME_SALES", special_ad_categories = Array.Empty<string>() }), false);
        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("status").GetString().Should().Be("PAUSED");
    }

    /// <summary>Ativação exige confirmação e campos desconhecidos são recusados.</summary>
    [Theory]
    [InlineData("{\"status\":\"ACTIVE\"}")]
    [InlineData("{\"access_token\":\"secret\"}")]
    [InlineData("{\"daily_budget\":100.50}")]
    [InlineData("{\"daily_budget\":1000,\"lifetime_budget\":10000}")]
    public void Validate_UnsafeOrInvalidInput_IsRejected(string json)
    {
        using var doc = JsonDocument.Parse(json);
        MetaWriteValidation.Validate("campaigns", "11", doc.RootElement, false).IsFailure.Should().BeTrue();
    }
}
