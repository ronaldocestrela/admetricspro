using BuildingBlocks.Domain.Primitives;

namespace Analytics.Domain.Attribution;

/// <summary>
/// Contrato de domínio para cálculo e comparação de modelos de atribuição multi-canal
/// (Primeiro Clique, Último Clique e Linear), incluindo mapeamento de conversões assistidas.
/// </summary>
public interface IAttributionCalculator
{
    /// <summary>
    /// Compara simultaneamente os modelos de Primeiro Clique, Último Clique e Linear para uma coleção de jornadas.
    /// </summary>
    /// <param name="journeys">Coleção de jornadas de conversão com seus respectivos pontos de contato.</param>
    /// <param name="channelCosts">Dicionário opcional contendo o investimento financeiro total por canal para cálculo de ROAS e CPA atribuídos.</param>
    /// <returns>Resultado comparativo side-by-side entre os três modelos de atribuição ou falha de negócio.</returns>
    Result<AttributionComparisonResult> CompareModels(
        IEnumerable<ConversionJourney> journeys,
        IReadOnlyDictionary<string, decimal>? channelCosts = null);

    /// <summary>
    /// Executa o cálculo de atribuição para um modelo específico selecionado.
    /// </summary>
    /// <param name="journeys">Coleção de jornadas de conversão com seus respectivos pontos de contato.</param>
    /// <param name="modelType">Modelo de atribuição a ser aplicado (FirstTouch, LastTouch ou Linear).</param>
    /// <param name="channelCosts">Dicionário opcional contendo o investimento total por canal para cálculo de ROAS e CPA atribuídos.</param>
    /// <returns>Lista com o detalhamento de atribuição por canal sob o modelo especificado ou falha de negócio.</returns>
    Result<IReadOnlyList<ChannelAttributionResult>> CalculateModel(
        IEnumerable<ConversionJourney> journeys,
        AttributionModelType modelType,
        IReadOnlyDictionary<string, decimal>? channelCosts = null);
}
