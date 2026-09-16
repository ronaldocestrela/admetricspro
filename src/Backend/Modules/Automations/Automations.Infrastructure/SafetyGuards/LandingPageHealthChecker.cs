using Automations.Domain.SafetyGuards;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Automations.SafetyGuards;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Automations.Infrastructure.SafetyGuards;

/// <summary>
/// Implementação concreta do serviço de verificação de integridade de Landing Pages (Detector 404/500).
/// Audita periodicamente as URLs finais dos anúncios ativos e pausa preventivamente os que apresentarem falhas.
/// </summary>
public sealed class LandingPageHealthChecker : ILandingPageHealthChecker
{
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly ISender _sender;
    private readonly ISecurityAlertNotifier _alertNotifier;
    private readonly IHttpLandingPageVerifier _httpVerifier;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="LandingPageHealthChecker"/>.
    /// </summary>
    public LandingPageHealthChecker(
        ITenantDbContextAccessor contextAccessor,
        ISender sender,
        ISecurityAlertNotifier alertNotifier,
        IHttpLandingPageVerifier httpVerifier)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _alertNotifier = alertNotifier ?? throw new ArgumentNullException(nameof(alertNotifier));
        _httpVerifier = httpVerifier ?? throw new ArgumentNullException(nameof(httpVerifier));
    }

    /// <inheritdoc />
    public async Task<Result<LandingPageCheckResult>> CheckAndMitigateLandingPagesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<LandingPageCheckResult>.Failure(
                Error.Validation("LandingPageHealthChecker.InvalidWorkspace", "O identificador do workspace é obrigatório."));
        }

        var dbContextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (dbContextResult.IsFailure)
        {
            return Result<LandingPageCheckResult>.Failure(dbContextResult.Error);
        }

        var db = dbContextResult.Value;

        // 1. Carregar anúncios ativos que possuam DestinationUrl preenchida
        var activeAds = await db.Ads
            .Where(a => a.Status == AdStatus.Active &&
                        a.DestinationUrl != null &&
                        a.DestinationUrl != "")
            .ToListAsync(cancellationToken);

        // Filtrar URLs em branco na memória após carregamento
        var candidates = activeAds
            .Where(a => !string.IsNullOrWhiteSpace(a.DestinationUrl))
            .ToList();

        if (candidates.Count == 0)
        {
            return Result<LandingPageCheckResult>.Success(new LandingPageCheckResult(
                workspaceId,
                EvaluatedAdsCount: 0,
                HealthyAdsCount: 0,
                BrokenAdsCount: 0,
                PausedAdsCount: 0,
                Incidents: Array.Empty<SafetyGuardIncident>()));
        }

        var evaluatedCount = candidates.Count;
        var healthyCount = 0;
        var brokenCount = 0;
        var pausedCount = 0;
        var incidents = new List<SafetyGuardIncident>();

        foreach (var ad in candidates)
        {
            var probeResult = await _httpVerifier.ProbeUrlAsync(ad.DestinationUrl!, cancellationToken);

            if (probeResult.IsHealthy)
            {
                healthyCount++;
            }
            else
            {
                brokenCount++;
                var reason = probeResult.StatusCode.HasValue
                    ? $"Trava de Segurança: Landing Page retornou erro HTTP {probeResult.StatusCode.Value} ({probeResult.ErrorMessage}) na URL '{ad.DestinationUrl}'."
                    : $"Trava de Segurança: Landing Page inacessível ({probeResult.ErrorMessage}) na URL '{ad.DestinationUrl}'.";

                // 2. Despachar comando in-memory PauseAdCommand via MediatR
                var pauseCommand = new PauseAdCommand(workspaceId, ad.Id, reason);
                var pauseResult = await _sender.Send(pauseCommand, cancellationToken);
                if (pauseResult.IsSuccess)
                {
                    pausedCount++;
                }

                // 3. Registrar incidente de integridade
                var incidentResult = SafetyGuardIncident.CreateBrokenLandingPageIncident(
                    workspaceId,
                    ad.Name,
                    ad.Id,
                    "MultiPlatform",
                    ad.DestinationUrl!,
                    probeResult.StatusCode,
                    probeResult.ErrorMessage,
                    pauseResult.IsSuccess ? "Anúncio Pausado Preventivamente" : "Falha ao Pausar Anúncio");

                if (incidentResult.IsSuccess)
                {
                    var incident = incidentResult.Value;
                    incidents.Add(incident);
                    db.SafetyGuardIncidents.Add(incident);

                    // 4. Despachar alerta de emergência multi-canal
                    var alertPayload = new SafetyAlertPayload(
                        IncidentId: incident.Id,
                        WorkspaceId: workspaceId,
                        GuardType: SafetyGuardType.BrokenLandingPage,
                        Severity: SafetyAlertSeverity.Emergency,
                        Title: $"🔥 ALERTA DE EMERGÊNCIA: Landing Page Inacessível no Anúncio '{ad.Name}'",
                        Message: reason,
                        TargetEntityName: ad.Name,
                        TargetEntityId: ad.Id,
                        Platform: "MultiPlatform",
                        ActionTaken: incident.ActionTaken,
                        MetricsContext: new Dictionary<string, string>
                        {
                            { "DestinationUrl", ad.DestinationUrl! },
                            { "StatusCode", probeResult.StatusCode?.ToString() ?? "N/A" },
                            { "ErrorDetail", probeResult.ErrorMessage }
                        },
                        TimestampUtc: DateTime.UtcNow);

                    await _alertNotifier.DispatchAlertAsync(alertPayload, cancellationToken);
                }
            }
        }

        if (incidents.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result<LandingPageCheckResult>.Success(new LandingPageCheckResult(
            workspaceId,
            evaluatedCount,
            healthyCount,
            brokenCount,
            pausedCount,
            incidents));
    }
}
