using Analytics.Application.Creatives.DTOs;
using Analytics.Domain.Creatives;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Analytics.Application.Creatives.Queries.GetCreativeHubOverview;

/// <summary>
/// Consulta para obtenção da visão geral do Creative Hub de um workspace,
/// incluindo status de fadiga de todos os criativos e contadores analíticos.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="StartDateUtc">Data inicial opcional para a janela de 7 dias.</param>
/// <param name="EndDateUtc">Data final opcional para a janela de 7 dias.</param>
public sealed record GetCreativeHubOverviewQuery(
    Guid WorkspaceId,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null) : IRequest<Result<CreativeHubOverviewDto>>;

/// <summary>
/// Manipulador da consulta de visão geral do Creative Hub.
/// </summary>
public sealed class GetCreativeHubOverviewQueryHandler : IRequestHandler<GetCreativeHubOverviewQuery, Result<CreativeHubOverviewDto>>
{
    private readonly ICreativeHubDataProvider _dataProvider;
    private readonly IAdFatigueDetector _fatigueDetector;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetCreativeHubOverviewQueryHandler"/>.
    /// </summary>
    /// <param name="dataProvider">Provedor de dados de criativos e métricas.</param>
    /// <param name="fatigueDetector">Motor analítico de detecção de fadiga.</param>
    public GetCreativeHubOverviewQueryHandler(
        ICreativeHubDataProvider dataProvider,
        IAdFatigueDetector fatigueDetector)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _fatigueDetector = fatigueDetector ?? throw new ArgumentNullException(nameof(fatigueDetector));
    }

    /// <inheritdoc />
    public async Task<Result<CreativeHubOverviewDto>> Handle(
        GetCreativeHubOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<CreativeHubOverviewDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do workspace é obrigatório."));
        }

        var adsResult = await _dataProvider.GetWorkspaceActiveAdsAsync(request.WorkspaceId, cancellationToken);
        if (adsResult.IsFailure)
        {
            return Result<CreativeHubOverviewDto>.Failure(adsResult.Error);
        }

        var activeAds = adsResult.Value;
        var endDate = request.EndDateUtc?.Date ?? DateTime.UtcNow.Date;
        var startDate = request.StartDateUtc?.Date ?? endDate.AddDays(-6);

        var creativeDtos = new List<CreativeFatigueDto>();

        foreach (var ad in activeAds)
        {
            var metricsResult = await _dataProvider.GetAdDailyMetricsAsync(
                request.WorkspaceId,
                ad.AdId,
                startDate,
                endDate,
                cancellationToken);

            if (metricsResult.IsFailure || metricsResult.Value.Count < 3)
            {
                // Se o criativo for muito recente ou sem dados de 3 dias, inclui como Healthy informativo
                creativeDtos.Add(new CreativeFatigueDto(
                    ad.AdId,
                    ad.Name,
                    ad.Platform,
                    ad.PreviewUrl,
                    CreativeFatigueStatus.Healthy.ToString(),
                    metricsResult.IsSuccess ? metricsResult.Value.Count : 0,
                    0m,
                    0m,
                    0m,
                    0m,
                    1.0m,
                    1.0m,
                    false,
                    "Período de dados inferior ao mínimo de 3 dias para diagnóstico de fadiga.",
                    "Aguarde mais veiculações para consolidação estatística."));
                continue;
            }

            var analysis = _fatigueDetector.AnalyzeFatigue(
                ad.AdId,
                ad.Name,
                ad.Platform,
                ad.PreviewUrl,
                metricsResult.Value);

            if (analysis.IsSuccess)
            {
                var f = analysis.Value;
                creativeDtos.Add(new CreativeFatigueDto(
                    f.AdId,
                    f.AdName,
                    f.Platform,
                    f.PreviewUrl,
                    f.Status.ToString(),
                    f.AnalyzedDays,
                    f.InitialCtr,
                    f.CurrentCtr,
                    f.CtrChangePercentage,
                    f.CtrTrendSlope,
                    f.AverageFrequency,
                    f.CurrentFrequency,
                    f.ReplacementSuggested,
                    f.Reason,
                    f.ActionRecommendation));
            }
        }

        var total = creativeDtos.Count;
        var fatigued = creativeDtos.Count(c => c.Status == CreativeFatigueStatus.Fatigued.ToString());
        var warning = creativeDtos.Count(c => c.Status == CreativeFatigueStatus.Warning.ToString());
        var healthy = creativeDtos.Count(c => c.Status == CreativeFatigueStatus.Healthy.ToString());
        var replacementSuggested = creativeDtos.Count(c => c.ReplacementSuggested);

        var overview = new CreativeHubOverviewDto(
            request.WorkspaceId,
            total,
            fatigued,
            warning,
            healthy,
            replacementSuggested,
            creativeDtos);

        return Result<CreativeHubOverviewDto>.Success(overview);
    }
}
