using System.Text.Json;
using FluentAssertions;
using Integrations.Application.Meta;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Garante orçamento por conjunto com escolha explícita de compartilhamento.</summary>
public sealed class MetaCampaignBudgetSharingTests
{
    /// <summary>Criação sem orçamento de campanha envia false por padrão sem ativação.</summary>
    [Fact]
    public void CreateCampaign_WithoutCampaignBudget_DefaultsSharingToFalse()
    {
        var result = MetaWriteValidation.Validate("campaigns", null, JsonSerializer.SerializeToElement(new
        { name = "Campanha", objective = "OUTCOME_TRAFFIC", special_ad_categories = Array.Empty<string>() }), false);
        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("is_adset_budget_sharing_enabled").GetBoolean().Should().BeFalse();
    }

    /// <summary>Aceita booleanos explícitos e rejeita representações textuais ambíguas.</summary>
    /// <param name="value">Representação JSON do campo.</param>
    /// <param name="valid">Resultado esperado.</param>
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", true)]
    [InlineData("\"true\"", false)]
    [InlineData("1", false)]
    public void CreateCampaign_ValidatesBooleanSharing(string value, bool valid)
    {
        var json = "{\"name\":\"Campanha\",\"objective\":\"OUTCOME_TRAFFIC\",\"special_ad_categories\":[],\"is_adset_budget_sharing_enabled\":" + value + "}";
        MetaWriteValidation.Validate("campaigns", null, JsonDocument.Parse(json).RootElement, false).IsSuccess.Should().Be(valid);
    }
}
