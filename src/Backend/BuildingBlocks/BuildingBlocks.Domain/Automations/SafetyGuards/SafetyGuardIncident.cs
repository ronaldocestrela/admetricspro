using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Automations.SafetyGuards;

/// <summary>
/// Entidade de domínio que registra um incidente de trava de segurança operacional (Overspending ou BrokenLandingPage).
/// Permite auditoria histórica, visualização no painel do gestor e rastreabilidade de ações preventivas automáticas.
/// </summary>
public sealed class SafetyGuardIncident : Entity<Guid>
{
    private SafetyGuardIncident(
        Guid id,
        Guid workspaceId,
        SafetyGuardType guardType,
        SafetyAlertSeverity severity,
        SafetyIncidentStatus status,
        string targetEntityName,
        Guid targetEntityId,
        string platform,
        string actionTaken,
        string reason,
        decimal? currentSpend,
        decimal? dailyBudget,
        int? httpStatusCode,
        string? targetUrl,
        DateTime detectedAtUtc,
        DateTime? resolvedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        GuardType = guardType;
        Severity = severity;
        Status = status;
        TargetEntityName = targetEntityName;
        TargetEntityId = targetEntityId;
        Platform = platform;
        ActionTaken = actionTaken;
        Reason = reason;
        CurrentSpend = currentSpend;
        DailyBudget = dailyBudget;
        HttpStatusCode = httpStatusCode;
        TargetUrl = targetUrl;
        DetectedAtUtc = detectedAtUtc;
        ResolvedAtUtc = resolvedAtUtc;
    }

    private SafetyGuardIncident()
        : base(Guid.Empty)
    {
        TargetEntityName = string.Empty;
        Platform = string.Empty;
        ActionTaken = string.Empty;
        Reason = string.Empty;
    }

    /// <summary>
    /// Obtém o identificador do workspace associado ao incidente.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o tipo da trava de segurança disparada.
    /// </summary>
    public SafetyGuardType GuardType { get; private set; }

    /// <summary>
    /// Obtém a severidade do incidente.
    /// </summary>
    public SafetyAlertSeverity Severity { get; private set; }

    /// <summary>
    /// Obtém o status operacional do incidente.
    /// </summary>
    public SafetyIncidentStatus Status { get; private set; }

    /// <summary>
    /// Obtém o nome descritivo da campanha ou anúncio afetado.
    /// </summary>
    public string TargetEntityName { get; private set; }

    /// <summary>
    /// Obtém o identificador único da campanha ou anúncio afetado.
    /// </summary>
    public Guid TargetEntityId { get; private set; }

    /// <summary>
    /// Obtém o nome da plataforma de mídia (MetaAds, GoogleAds, etc.).
    /// </summary>
    public string Platform { get; private set; }

    /// <summary>
    /// Obtém a ação preventiva executada pelo sistema (ex: Campanha Pausada).
    /// </summary>
    public string ActionTaken { get; private set; }

    /// <summary>
    /// Obtém a justificativa detalhada ou mensagem do motivo da trava.
    /// </summary>
    public string Reason { get; private set; }

    /// <summary>
    /// Obtém o gasto acumulado no momento da detecção (quando aplicável).
    /// </summary>
    public decimal? CurrentSpend { get; private set; }

    /// <summary>
    /// Obtém o orçamento diário configurado (quando aplicável).
    /// </summary>
    public decimal? DailyBudget { get; private set; }

    /// <summary>
    /// Obtém o código de status HTTP retornado na verificação (quando aplicável).
    /// </summary>
    public int? HttpStatusCode { get; private set; }

    /// <summary>
    /// Obtém a URL de destino da Landing Page verificada (quando aplicável).
    /// </summary>
    public string? TargetUrl { get; private set; }

    /// <summary>
    /// Obtém a data e hora UTC em que o incidente foi detectado.
    /// </summary>
    public DateTime DetectedAtUtc { get; private set; }

    /// <summary>
    /// Obtém a data e hora UTC em que o incidente foi resolvido ou reconhecido.
    /// </summary>
    public DateTime? ResolvedAtUtc { get; private set; }

    /// <summary>
    /// Cria um novo incidente de Overspending com dados validados.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="campaignName">Nome da campanha.</param>
    /// <param name="campaignId">Identificador da campanha.</param>
    /// <param name="platform">Plataforma de anúncios.</param>
    /// <param name="currentSpend">Gasto diário consumido.</param>
    /// <param name="dailyBudget">Orçamento diário programado.</param>
    /// <param name="actionTaken">Ação de mitigação executada.</param>
    /// <returns>Resultado contendo a nova instância de incidente.</returns>
    public static Result<SafetyGuardIncident> CreateOverspendingIncident(
        Guid workspaceId,
        string campaignName,
        Guid campaignId,
        string platform,
        decimal currentSpend,
        decimal dailyBudget,
        string actionTaken = "Campanha Pausada Preventivamente")
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<SafetyGuardIncident>.Failure(
                Error.Validation("SafetyGuard.InvalidWorkspace", "O workspace é obrigatório para registrar o incidente."));
        }

        if (campaignId == Guid.Empty)
        {
            return Result<SafetyGuardIncident>.Failure(
                Error.Validation("SafetyGuard.InvalidCampaign", "O identificador da campanha é obrigatório."));
        }

        var percentage = dailyBudget > 0 ? (currentSpend / dailyBudget) * 100m : 0m;
        var reason = $"Gasto diário de R$ {currentSpend:F2} superou 120% do orçamento programado de R$ {dailyBudget:F2} ({percentage:F1}% atingido).";

        var incident = new SafetyGuardIncident(
            Guid.NewGuid(),
            workspaceId,
            SafetyGuardType.Overspending,
            SafetyAlertSeverity.Critical,
            SafetyIncidentStatus.Mitigated,
            string.IsNullOrWhiteSpace(campaignName) ? "Campanha Sem Nome" : campaignName,
            campaignId,
            string.IsNullOrWhiteSpace(platform) ? "Unknown" : platform,
            actionTaken,
            reason,
            currentSpend,
            dailyBudget,
            null,
            null,
            DateTime.UtcNow,
            null);

        return Result<SafetyGuardIncident>.Success(incident);
    }

    /// <summary>
    /// Cria um novo incidente de Landing Page Quebrada (404/500/Timeout) com dados validados.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="adName">Nome do anúncio.</param>
    /// <param name="adId">Identificador do anúncio.</param>
    /// <param name="platform">Plataforma de mídia.</param>
    /// <param name="targetUrl">URL de destino verificada.</param>
    /// <param name="httpStatusCode">Código de status HTTP ou nulo em falha de conexão/timeout.</param>
    /// <param name="errorMessage">Descrição do erro ou falha.</param>
    /// <param name="actionTaken">Ação preventiva executada.</param>
    /// <returns>Resultado contendo a nova instância de incidente.</returns>
    public static Result<SafetyGuardIncident> CreateBrokenLandingPageIncident(
        Guid workspaceId,
        string adName,
        Guid adId,
        string platform,
        string targetUrl,
        int? httpStatusCode,
        string errorMessage,
        string actionTaken = "Anúncio Pausado Preventivamente")
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<SafetyGuardIncident>.Failure(
                Error.Validation("SafetyGuard.InvalidWorkspace", "O workspace é obrigatório para registrar o incidente."));
        }

        if (adId == Guid.Empty)
        {
            return Result<SafetyGuardIncident>.Failure(
                Error.Validation("SafetyGuard.InvalidAd", "O identificador do anúncio é obrigatório."));
        }

        var reason = httpStatusCode.HasValue
            ? $"A Landing Page retornou código HTTP {httpStatusCode.Value} ({errorMessage})."
            : $"Falha ao acessar Landing Page: {errorMessage}";

        var incident = new SafetyGuardIncident(
            Guid.NewGuid(),
            workspaceId,
            SafetyGuardType.BrokenLandingPage,
            SafetyAlertSeverity.Emergency,
            SafetyIncidentStatus.Mitigated,
            string.IsNullOrWhiteSpace(adName) ? "Anúncio Sem Nome" : adName,
            adId,
            string.IsNullOrWhiteSpace(platform) ? "Unknown" : platform,
            actionTaken,
            reason,
            null,
            null,
            httpStatusCode,
            targetUrl,
            DateTime.UtcNow,
            null);

        return Result<SafetyGuardIncident>.Success(incident);
    }

    /// <summary>
    /// Marca o incidente como reconhecido pelo usuário.
    /// </summary>
    public void MarkAsAcknowledged()
    {
        Status = SafetyIncidentStatus.Acknowledged;
    }

    /// <summary>
    /// Marca o incidente como resolvido.
    /// </summary>
    public void MarkAsResolved()
    {
        Status = SafetyIncidentStatus.Resolved;
        ResolvedAtUtc = DateTime.UtcNow;
    }
}
