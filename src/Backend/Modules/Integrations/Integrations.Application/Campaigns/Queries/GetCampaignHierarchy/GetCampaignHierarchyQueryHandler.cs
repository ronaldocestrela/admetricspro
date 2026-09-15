using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Campaigns.DTOs;
using Integrations.Domain.Campaigns;

namespace Integrations.Application.Campaigns.Queries.GetCampaignHierarchy;

/// <summary>
/// Manipulador da consulta <see cref="GetCampaignHierarchyQuery"/>.
/// Carrega a estrutura de campanhas, conjuntos e anúncios subordinados a partir do TenantDbContext.
/// </summary>
public sealed class GetCampaignHierarchyQueryHandler : IQueryHandler<GetCampaignHierarchyQuery, IReadOnlyList<CampaignHierarchyDto>>
{
    private readonly ICampaignHierarchyRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetCampaignHierarchyQueryHandler"/>.
    /// </summary>
    /// <param name="repository">Repositório de hierarquia de campanhas.</param>
    public GetCampaignHierarchyQueryHandler(ICampaignHierarchyRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CampaignHierarchyDto>>> Handle(
        GetCampaignHierarchyQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<IReadOnlyList<CampaignHierarchyDto>>.Failure(
                Error.Validation("GetCampaignHierarchy.EmptyWorkspaceId", "O workspace é obrigatório."));
        }

        if (request.CampaignId.HasValue && request.CampaignId.Value != Guid.Empty)
        {
            var single = await _repository.GetCampaignWithHierarchyByIdAsync(request.CampaignId.Value, cancellationToken);
            if (single is null || single.WorkspaceId != request.WorkspaceId)
            {
                return Result<IReadOnlyList<CampaignHierarchyDto>>.Success(Array.Empty<CampaignHierarchyDto>());
            }

            var singleDto = MapToDto(single);
            return Result<IReadOnlyList<CampaignHierarchyDto>>.Success(new[] { singleDto });
        }

        var campaigns = await _repository.GetCampaignsByWorkspaceAsync(
            request.WorkspaceId,
            request.ConnectedAdAccountId,
            request.Platform,
            request.Status,
            cancellationToken);

        var dtos = campaigns.Select(MapToDto).ToList();
        return Result<IReadOnlyList<CampaignHierarchyDto>>.Success(dtos);
    }

    private static CampaignHierarchyDto MapToDto(BuildingBlocks.Domain.Campaigns.Campaign campaign)
    {
        var adSets = campaign.AdSets.Select(set => new AdSetHierarchyDto(
            set.Id,
            set.CampaignId,
            set.ExternalAdSetId,
            set.Name,
            set.Status.ToString(),
            set.BidStrategy,
            set.OptimizationGoal,
            set.DailyBudget,
            set.LifetimeBudget,
            set.TargetingSummary,
            set.Ads.Select(ad => new AdHierarchyDto(
                ad.Id,
                ad.AdSetId,
                ad.ExternalAdId,
                ad.Name,
                ad.Status.ToString(),
                ad.CreativeType.ToString(),
                ad.Headline,
                ad.Body,
                ad.DestinationUrl,
                ad.PreviewUrl,
                ad.CallToAction)).ToList()
        )).ToList();

        return new CampaignHierarchyDto(
            campaign.Id,
            campaign.WorkspaceId,
            campaign.ConnectedAdAccountId,
            campaign.Platform,
            campaign.ExternalCampaignId,
            campaign.Name,
            campaign.Status.ToString(),
            campaign.Objective,
            campaign.DailyBudget,
            campaign.LifetimeBudget,
            campaign.Currency,
            campaign.StartDateUtc,
            campaign.EndDateUtc,
            campaign.LastSyncedAtUtc,
            adSets);
    }
}
