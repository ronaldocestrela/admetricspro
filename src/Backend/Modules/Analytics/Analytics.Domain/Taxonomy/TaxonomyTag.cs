namespace Analytics.Domain.Taxonomy;

/// <summary>
/// Representa uma tag categórica ou marcador semântico extraído da nomenclatura de marketing.
/// </summary>
/// <param name="Key">Chave ou dimensão da taxonomia (ex: Funil, Publico, Formato, Objetivo).</param>
/// <param name="Value">Valor identificado (ex: Topo de Funil, Lookalike 1%, Vídeo, Conversão).</param>
/// <param name="RawToken">Token ou substring original encontrada no nome (ex: [TOF], LAL 1%, VID01).</param>
public sealed record TaxonomyTag(
    string Key,
    string Value,
    string RawToken);
