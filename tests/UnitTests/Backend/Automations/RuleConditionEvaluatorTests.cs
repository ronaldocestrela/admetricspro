using BuildingBlocks.Domain.Automations;
using Automations.Domain.Services;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o motor avaliador de condições e predicados da DSL (Subfase 4.1.1 - TDD).
/// Valida gatilhos combinados cross-platform, operadores booleanos e proteções numéricas.
/// </summary>
public sealed class RuleConditionEvaluatorTests
{
    private readonly RuleConditionEvaluator _evaluator = new();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Valida que o gatilho combinado cross-platform dispara com sucesso quando TikTok CPA &gt; 50 e Google ROAS &gt; 4.5.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldTrigger_WhenCombinedTriggerMatches_TikTokCpaGreaterThan50AndGoogleRoasGreaterThan4Point5()
    {
        // Cenário do roadmap 4.1.1:
        // "Se CPA do TikTok Ads > R$ 50 nas últimas 48h E Google Ads ROAS > 4.5 -> Deve disparar"
        var tiktokCampaignId = Guid.NewGuid();
        var googleCampaignId = Guid.NewGuid();

        var tiktokCondition = new MetricPredicate(
            platform: "TikTokAds",
            scope: RuleScope.Campaign,
            metric: MetricType.Cpa,
            @operator: ComparisonOperator.GreaterThan,
            threshold: 50.0m,
            timeWindowHours: 48,
            targetEntityId: tiktokCampaignId);

        var googleCondition = new MetricPredicate(
            platform: "GoogleAds",
            scope: RuleScope.Campaign,
            metric: MetricType.Roas,
            @operator: ComparisonOperator.GreaterThan,
            threshold: 4.5m,
            timeWindowHours: 48,
            targetEntityId: googleCampaignId);

        var rootGroup = new RuleConditionGroup(LogicalOperator.And, new IRuleCondition[] { tiktokCondition, googleCondition });

        // Monta o contexto em memória:
        // TikTok: Spend = 120, Conversions = 2 => CPA = 60.00 (> 50)
        // Google: Spend = 100, ConversionValue = 500 => ROAS = 5.0 (> 4.5)
        var context = new RuleEvaluationContext(_workspaceId, new[]
        {
            new PerformanceMetricSnapshot("TikTokAds", RuleScope.Campaign, tiktokCampaignId, 48, 120m, 5000, 200, 2m, 100m),
            new PerformanceMetricSnapshot("GoogleAds", RuleScope.Campaign, googleCampaignId, 48, 100m, 4000, 300, 10m, 500m)
        });

        // Act
        var result = _evaluator.Evaluate(rootGroup, context);

        // Assert
        result.IsTriggered.Should().BeTrue();
        result.EvaluatedPredicatesCount.Should().Be(2);
        result.MatchedEntityIds.Should().Contain(tiktokCampaignId);
        result.MatchedEntityIds.Should().Contain(googleCampaignId);
    }

    /// <summary>
    /// Valida que a condição lógica AND não dispara quando uma das plataformas não atinge o limiar estipulado.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldNotTrigger_WhenAndConditionFailsForOnePlatform()
    {
        // TikTok CPA > 50, mas Google ROAS é 3.0 (< 4.5)
        var tiktokCampaignId = Guid.NewGuid();
        var googleCampaignId = Guid.NewGuid();

        var rootGroup = new RuleConditionGroup(LogicalOperator.And, new IRuleCondition[]
        {
            new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50.0m, 48, tiktokCampaignId),
            new MetricPredicate("GoogleAds", RuleScope.Campaign, MetricType.Roas, ComparisonOperator.GreaterThan, 4.5m, 48, googleCampaignId)
        });

        var context = new RuleEvaluationContext(_workspaceId, new[]
        {
            new PerformanceMetricSnapshot("TikTokAds", RuleScope.Campaign, tiktokCampaignId, 48, 120m, 5000, 200, 2m, 100m), // CPA = 60
            new PerformanceMetricSnapshot("GoogleAds", RuleScope.Campaign, googleCampaignId, 48, 100m, 4000, 300, 5m, 300m)  // ROAS = 3.0 (< 4.5)
        });

        var result = _evaluator.Evaluate(rootGroup, context);

        result.IsTriggered.Should().BeFalse();
    }

    /// <summary>
    /// Valida que a condição lógica OR dispara quando ao menos uma das condições é satisfeita.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldTrigger_WhenOrConditionMatchesAtLeastOne()
    {
        var metaCampaignId = Guid.NewGuid();
        var googleCampaignId = Guid.NewGuid();

        var rootGroup = new RuleConditionGroup(LogicalOperator.Or, new IRuleCondition[]
        {
            new MetricPredicate("MetaAds", RuleScope.Campaign, MetricType.Ctr, ComparisonOperator.LessThan, 1.0m, 24, metaCampaignId),
            new MetricPredicate("GoogleAds", RuleScope.Campaign, MetricType.Cpc, ComparisonOperator.GreaterThan, 10.0m, 24, googleCampaignId)
        });

        // Meta CTR = 0.5% (< 1.0) -> Verdadeiro
        // Google CPC = 2.0 (<= 10.0) -> Falso
        var context = new RuleEvaluationContext(_workspaceId, new[]
        {
            new PerformanceMetricSnapshot("MetaAds", RuleScope.Campaign, metaCampaignId, 24, 50m, 10000, 50, 2m, 80m), // CTR = 0.5%
            new PerformanceMetricSnapshot("GoogleAds", RuleScope.Campaign, googleCampaignId, 24, 100m, 2000, 50, 5m, 250m)  // CPC = 2.0
        });

        var result = _evaluator.Evaluate(rootGroup, context);

        result.IsTriggered.Should().BeTrue();
    }

    /// <summary>
    /// Valida que grupos compostos aninhados combinando AND e OR são avaliados corretamente.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldSupport_NestedCompositeGroup_WithAndAndOr()
    {
        // Árvore: (TikTok CPA > 50 AND Google ROAS > 4.5) OR (Meta CTR < 1.0)
        var tiktokId = Guid.NewGuid();
        var googleId = Guid.NewGuid();
        var metaId = Guid.NewGuid();

        var andGroup = new RuleConditionGroup(LogicalOperator.And, new IRuleCondition[]
        {
            new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48, tiktokId),
            new MetricPredicate("GoogleAds", RuleScope.Campaign, MetricType.Roas, ComparisonOperator.GreaterThan, 4.5m, 48, googleId)
        });

        var metaCondition = new MetricPredicate("MetaAds", RuleScope.Campaign, MetricType.Ctr, ComparisonOperator.LessThan, 1.0m, 24, metaId);

        var rootOr = new RuleConditionGroup(LogicalOperator.Or, new IRuleCondition[] { andGroup, metaCondition });

        // Cenário: O grupo AND falha (Google ROAS = 2.0), mas Meta CTR = 0.8% (< 1.0%) satisfaz o OR
        var context = new RuleEvaluationContext(_workspaceId, new[]
        {
            new PerformanceMetricSnapshot("TikTokAds", RuleScope.Campaign, tiktokId, 48, 120m, 5000, 200, 2m, 100m),
            new PerformanceMetricSnapshot("GoogleAds", RuleScope.Campaign, googleId, 48, 100m, 4000, 300, 5m, 200m), // ROAS 2.0
            new PerformanceMetricSnapshot("MetaAds", RuleScope.Campaign, metaId, 24, 100m, 10000, 80, 5m, 300m)       // CTR 0.8%
        });

        var result = _evaluator.Evaluate(rootOr, context);

        result.IsTriggered.Should().BeTrue();
        result.MatchedEntityIds.Should().Contain(metaId);
    }

    /// <summary>
    /// Valida que o operador unário NOT inverte a avaliação da condição subordinada.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldRespect_NotOperator()
    {
        var campaignId = Guid.NewGuid();
        var innerPredicate = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 24, campaignId);
        var notGroup = new RuleConditionGroup(LogicalOperator.Not, new IRuleCondition[] { innerPredicate });

        // CPA = 30 (não é maior que 50 => inner é falso => NOT(falso) é verdadeiro)
        var context = new RuleEvaluationContext(_workspaceId, new[]
        {
            new PerformanceMetricSnapshot("TikTokAds", RuleScope.Campaign, campaignId, 24, 60m, 2000, 100, 2m, 150m) // CPA = 30
        });

        var result = _evaluator.Evaluate(notGroup, context);

        result.IsTriggered.Should().BeTrue();
    }

    /// <summary>
    /// Valida que métricas fora da janela temporal estipulada pela regra não são computadas.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldNotMatch_WhenTimeWindowDiffers()
    {
        var campaignId = Guid.NewGuid();
        // Regra exige janela de 48h
        var predicate = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48, campaignId);
        var root = new RuleConditionGroup(LogicalOperator.And, new IRuleCondition[] { predicate });

        // Contexto só possui snapshot de 72h
        var context = new RuleEvaluationContext(_workspaceId, new[]
        {
            new PerformanceMetricSnapshot("TikTokAds", RuleScope.Campaign, campaignId, 72, 120m, 5000, 200, 2m, 100m)
        });

        var result = _evaluator.Evaluate(root, context);

        result.IsTriggered.Should().BeFalse();
    }

    /// <summary>
    /// Valida que o cálculo de KPIs é seguro contra divisões por zero quando Spend ou Conversions forem nulos.
    /// </summary>
    [Fact]
    public void Evaluate_ShouldHandleDivisionByZeroSafely_WhenZeroConversionsOrZeroSpend()
    {
        var campaignId = Guid.NewGuid();
        var snapshot = new PerformanceMetricSnapshot("MetaAds", RuleScope.Campaign, campaignId, 24, spend: 0m, impressions: 0, clicks: 0, conversions: 0m, conversionValue: 0m);

        // CPA, ROAS, CPC, CPM, CTR devem ser 0 sem disparar exceção
        snapshot.Cpa.Should().Be(0m);
        snapshot.Roas.Should().Be(0m);
        snapshot.Cpc.Should().Be(0m);
        snapshot.Cpm.Should().Be(0m);
        snapshot.Ctr.Should().Be(0m);

        var predicate = new MetricPredicate("MetaAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 0m, 24, campaignId);
        var root = new RuleConditionGroup(LogicalOperator.And, new IRuleCondition[] { predicate });
        var context = new RuleEvaluationContext(_workspaceId, new[] { snapshot });

        var result = _evaluator.Evaluate(root, context);

        result.IsTriggered.Should().BeFalse();
    }
}
