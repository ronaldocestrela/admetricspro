using Analytics.Application.Creatives.DTOs;
using Analytics.Domain.Creatives;
using BuildingBlocks.Domain.Primitives;
using MediatR;

namespace Analytics.Application.Creatives.Queries.GetCreativeFatigueAnalysis;

/// <summary>
/// Consulta para obtenção do diagnóstico de fadiga de um criativo específico em um workspace.
/// </summary>
/// <param name="WorkspaceId">Identificador único do workspace.</param>
/// <param name="AdId">Identificador único do anúncio / criativo.</param>
/// <param name="StartDateUtc">Data inicial opcional (padrão últimos 7 dias).</param>
/// <param name="EndDateUtc">Data final opcional (padrão data corrente UTC).</param>
public sealed record GetCreativeFatigueAnalysisQuery(
    Guid WorkspaceId,
    Guid AdId,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null) : IRequest<Result<CreativeFatigueDto>>;

/// <summary>
/// Manipulador da consulta de diagnóstico de fadiga de criativo.
/// </summary>
public sealed class GetCreativeFatigueAnalysisQueryHandler : IRequestHandler<GetCreativeFatigueAnalysisQuery, Result<CreativeFatigueDto>>
{
    private readonly ICreativeHubDataProvider _dataProvider;
    private readonly IAdFatigueDetector _fatigueDetector;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetCreativeFatigueAnalysisQueryHandler"/>.
    /// </summary>
    /// <param name="dataProvider">Provedor de dados de criativos e métricas.</param>
    /// <param name="fatigueDetector">Motor de detecção de fadiga.</param>
    public GetCreativeFatigueAnalysisQueryHandler(
        ICreativeHubDataProvider dataProvider,
        IAdFatigueDetector fatigueDetector)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _fatigueDetector = fatigueDetector ?? throw new ArgumentNullException(nameof(fatigueDetector));
    }

    /// <inheritdoc />
    public async Task<Result<CreativeFatigueDto>> Handle(
        GetCreativeFatigueAnalysisQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<CreativeFatigueDto>.Failure(
                Error.Validation("Workspace.InvalidId", "O identificador do workspace é obrigatório."));
        }

        if (request.AdId == Guid.Empty)
        {
            return Result<CreativeFatigueDto>.Failure(
                Error.Validation("Ad.InvalidId", "O identificador do anúncio é obrigatório."));
        }

        var endDate = request.EndDateUtc?.Date ?? DateTime.UtcNow.Date;
        var startDate = request.StartDateUtc?.Date ?? endDate.AddDays(-6);

        var metricsResult = await _dataProvider.GetAdDailyMetricsAsync(
            request.WorkspaceId,
            request.AdId,
            startDate,
            endDate,
            cancellationToken);

        if (metricsResult.IsFailure)
        {
            return Result<CreativeFatigueDto>.Failure(metricsResult.Error);
        }

        var adsResult = await _dataProvider.GetWorkspaceActiveAdsAsync(request.WorkspaceId, cancellationToken);
        if (adsResult.IsFailure)
        {
            return Result<CreativeFatigueDto>.Failure(adsResult.Error);
        }

        var adMetadata = adsResult.Value.FirstOrDefault(a => a.AdId == request.AdId);
        var adName = adMetadata?.Name ?? $"Anúncio {request.AdId}";
        var platform = adMetadata?.Platform ?? "Unknown";
        var previewUrl = adMetadata?.PreviewUrl;

        var analysisResult = _fatigueDetector.AnalyzeFatigue(
            request.AdId,
            adName,
            platform,
            previewUrl,
            metricsResult.Value);

        if (analysisResult.IsFailure)
        {
            return Result<CreativeFatigueDto>.Failure(analysisResult.Error);
        }

        var f = analysisResult.Value;
        var dto = new CreativeFatigueDto(
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
            f.ActionRecommendation);

        return Result<CreativeFatigueDto>.Success(dto);
    }
}
