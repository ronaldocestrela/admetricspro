namespace Integrations.Application.Campaigns.DTOs;

/// <summary>
/// Resumo executivo retornado após a execução da sincronização estrutural de campanhas.
/// </summary>
/// <param name="TotalAccountsProcessed">Quantidade de contas conectadas processadas.</param>
/// <param name="TotalCampaignsSynced">Quantidade total de campanhas inseridas ou atualizadas.</param>
/// <param name="TotalAdSetsSynced">Quantidade total de conjuntos ou grupos inseridos ou atualizados.</param>
/// <param name="TotalAdsSynced">Quantidade total de anúncios ou criativos inseridos ou atualizados.</param>
/// <param name="SyncedAtUtc">Carimbo UTC da conclusão da sincronização.</param>
/// <param name="SyncedPlatforms">Lista de plataformas sincronizadas com sucesso.</param>
public sealed record SyncCampaignHierarchySummaryDto(
    int TotalAccountsProcessed,
    int TotalCampaignsSynced,
    int TotalAdSetsSynced,
    int TotalAdsSynced,
    DateTime SyncedAtUtc,
    IReadOnlyList<string> SyncedPlatforms);
