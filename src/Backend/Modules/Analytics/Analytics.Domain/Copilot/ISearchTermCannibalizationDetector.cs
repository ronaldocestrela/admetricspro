namespace Analytics.Domain.Copilot;

/// <summary>
/// Contrato do detector de canibalização e disputa de termos de busca entre canais (Google Ads vs. Bing Ads) ou campanhas internas.
/// </summary>
public interface ISearchTermCannibalizationDetector
{
    /// <summary>
    /// Avalia a performance de palavras-chave e termos de pesquisa e detecta concorrência predatória com disparidade de CPC/CPA.
    /// </summary>
    /// <param name="keywords">Lista de palavras-chave com métricas por plataforma e campanha.</param>
    /// <returns>Lista de anomalias de canibalização detectadas ordenadas por severidade e valor desperdiçado.</returns>
    IReadOnlyList<SearchCannibalizationAnomaly> DetectCannibalization(IReadOnlyList<SearchKeywordPerformance> keywords);
}
