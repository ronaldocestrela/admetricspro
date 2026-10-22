using System.Text.Json;
using FluentAssertions;
using Integrations.Application.Meta;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Regressões do catálogo oficial versionado e da validação de configurações completas.</summary>
public sealed class MetaResourceConfigurationTests
{
    /// <summary>Zero explícito de edição é preservado e não tratado como valor ausente.</summary>
    [Fact]
    public void Validate_ExplicitZeroSpendCapOnUpdate_IsPreserved()
    {
        var result = MetaWriteValidation.Validate("campaigns", "123", JsonSerializer.Deserialize<JsonElement>("{\"spend_cap\":0}"), false);
        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("spend_cap").GetInt32().Should().Be(0);
    }
    /// <summary>Expõe tipos, enumerações e disponibilidade por operação sem aceitar mudança de conta.</summary>
    [Fact]
    public void Catalog_UsesOfficialOperationSpecificFields()
    {
        var campaign = MetaResourceCatalog.Get("campaigns").Value;
        campaign.ApiVersion.Should().Be("v26.0");
        campaign.Fields.Should().Contain(f => f.Name == "buying_type" && f.Create && !f.Update);
        campaign.Fields.Should().Contain(f => f.Name == "is_adset_budget_sharing_enabled" && f.Type == "bool");
        var sets = MetaResourceCatalog.Get("adsets").Value;
        sets.Fields.Should().Contain(f => f.Name == "destination_type" && f.EnumValues.Contains("WEBSITE"));
        sets.Fields.Should().Contain(f => f.Name == "dsa_beneficiary" && f.Update);
        sets.Fields.Single(f => f.Name == "account_id").Restriction.Should().NotBeNullOrEmpty();
        MetaResourceCatalog.Get("ads").Value.Fields.Should().Contain(f => f.Name == "creative_automation_spec");
        MetaResourceCatalog.Get("adcreatives").Value.Fields.Should().Contain(f => f.Name == "degrees_of_freedom_spec");
        MetaResourceCatalog.Get("unknown").IsFailure.Should().BeTrue();
    }

    /// <summary>Objetos, enums e programações são aceitos por recurso, com criação pausada.</summary>
    [Theory]
    [InlineData("campaigns", "{\"name\":\"Venda\",\"objective\":\"OUTCOME_SALES\",\"special_ad_categories\":[],\"buying_type\":\"AUCTION\",\"promoted_object\":{\"product_set_id\":\"123\"},\"bid_strategy\":\"LOWEST_COST_WITHOUT_CAP\"}")]
    [InlineData("adsets", "{\"name\":\"Público\",\"campaign_id\":\"123\",\"billing_event\":\"IMPRESSIONS\",\"optimization_goal\":\"OFFSITE_CONVERSIONS\",\"destination_type\":\"WEBSITE\",\"dsa_beneficiary\":\"Empresa\",\"attribution_spec\":[{\"event_type\":\"CLICK_THROUGH\",\"window_days\":7}],\"targeting\":{\"geo_locations\":{\"countries\":[\"BR\"]},\"publisher_platforms\":[\"facebook\",\"instagram\"]}}")]
    [InlineData("ads", "{\"name\":\"Anúncio\",\"adset_id\":\"123\",\"creative\":{\"creative_id\":\"456\"},\"conversion_domain\":\"example.test\",\"ad_schedule_start_time\":\"2026-11-01T12:00:00Z\"}")]
    public void Validate_ExtendedConfiguration_IsAccepted(string resource, string json)
    {
        var result = MetaWriteValidation.Validate(resource, null, JsonSerializer.Deserialize<JsonElement>(json), false);
        result.IsSuccess.Should().BeTrue();
        result.Value.GetProperty("status").GetString().Should().Be("PAUSED");
    }

    /// <summary>Campos de transporte, tipos incorretos e opções somente de criação são recusados.</summary>
    [Theory]
    [InlineData("campaigns", "{\"buying_type\":\"AUCTION\"}")]
    [InlineData("campaigns", "{\"bid_strategy\":\"invented\"}")]
    [InlineData("adsets", "{\"account_id\":\"999\"}")]
    [InlineData("adsets", "{\"is_dynamic_creative\":\"true\"}")]
    [InlineData("ads", "{\"priority\":1.25}")]
    [InlineData("ads", "{\"creative\":{\"creative_id\":\"123\",\"access_token\":\"secret\"}}")]
    public void Validate_InvalidExtendedUpdate_IsRejected(string resource, string json) =>
        MetaWriteValidation.Validate(resource, "123", JsonSerializer.Deserialize<JsonElement>(json), false).IsFailure.Should().BeTrue();
}
