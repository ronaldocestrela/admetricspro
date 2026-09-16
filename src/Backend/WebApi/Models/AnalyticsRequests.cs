namespace WebApi.Models;

/// <summary>
/// Parâmetros de requisição para conversão monetária individual.
/// </summary>
/// <param name="Amount">Valor financeiro a converter.</param>
/// <param name="SourceCurrency">Código ISO da moeda de origem (ex: USD, EUR, BRL).</param>
/// <param name="TargetCurrency">Código ISO da moeda de destino (ex: BRL, USD).</param>
/// <param name="Date">Data de referência histórica ou corrente para a cotação.</param>
public sealed record ConvertCurrencyApiRequest(
    decimal Amount,
    string SourceCurrency,
    string TargetCurrency,
    DateTime Date);

/// <summary>
/// Item individual de valor a converter em lote.
/// </summary>
/// <param name="Amount">Valor financeiro.</param>
/// <param name="SourceCurrency">Moeda de origem.</param>
/// <param name="Date">Data da cotação.</param>
public sealed record CurrencyBatchItemApiRequest(
    decimal Amount,
    string SourceCurrency,
    DateTime Date);

/// <summary>
/// Parâmetros de requisição para conversão monetária em lote.
/// </summary>
/// <param name="Items">Lista de itens a serem convertidos.</param>
/// <param name="TargetCurrency">Moeda de destino unificada.</param>
public sealed record ConvertCurrencyBatchApiRequest(
    IReadOnlyList<CurrencyBatchItemApiRequest> Items,
    string TargetCurrency);

/// <summary>
/// Parâmetros de requisição para classificação taxonômica individual.
/// </summary>
/// <param name="CampaignName">Nome da campanha de tráfego pago.</param>
/// <param name="AdSetName">Nome opcional do conjunto de anúncios.</param>
/// <param name="AdName">Nome opcional do anúncio / criativo.</param>
/// <param name="ReferenceId">Identificador opcional de correlação.</param>
public sealed record ClassifyTaxonomyApiRequest(
    string CampaignName,
    string? AdSetName = null,
    string? AdName = null,
    string? ReferenceId = null);

/// <summary>
/// Parâmetros de requisição para classificação taxonômica em lote.
/// </summary>
/// <param name="Items">Lista de campanhas ou anúncios a classificar.</param>
public sealed record BatchClassifyTaxonomyApiRequest(
    IReadOnlyList<ClassifyTaxonomyApiRequest> Items);
