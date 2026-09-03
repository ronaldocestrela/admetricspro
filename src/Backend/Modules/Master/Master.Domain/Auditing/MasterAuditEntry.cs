using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace Master.Domain.Auditing;

/// <summary>
/// Representa um registro agregado de trilha de auditoria global imutável (append-only) no Catálogo Master.
/// Armazena metadados de execução para todas as mutações e intervenções operacionais,
/// com suporte dedicado e indexado ao modo de personificação (Shadow Mode).
/// </summary>
public sealed class MasterAuditEntry : AggregateRoot<Guid>
{
    private readonly List<string> _tags = [];

    private MasterAuditEntry(
        Guid id,
        Guid? tenantId,
        string action,
        string resource,
        string? resourceId,
        string? details,
        bool isImpersonated,
        Guid? superAdminId,
        string? supportTicketId,
        Guid? impersonationSessionId,
        string? ipAddress,
        DateTime createdAtUtc,
        IEnumerable<string> tags)
        : base(id)
    {
        TenantId = tenantId;
        Action = action;
        Resource = resource;
        ResourceId = resourceId;
        Details = details;
        IsImpersonated = isImpersonated;
        SuperAdminId = superAdminId;
        SupportTicketId = supportTicketId;
        ImpersonationSessionId = impersonationSessionId;
        IpAddress = ipAddress;
        CreatedAtUtc = createdAtUtc;

        foreach (var tag in tags)
        {
            if (!string.IsNullOrWhiteSpace(tag) && !_tags.Contains(tag.Trim()))
            {
                _tags.Add(tag.Trim());
            }
        }
    }

    private MasterAuditEntry()
        : base(Guid.NewGuid())
    {
        Action = string.Empty;
        Resource = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do Tenant associado à operação, se aplicável.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Obtém o nome semântico da ação executada (ex.: "Tenant.Update", "Campaign.Pause").
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Obtém o recurso ou tipo de entidade alvo da ação (ex.: "Tenant", "Campaign", "Plan").
    /// </summary>
    public string Resource { get; private set; }

    /// <summary>
    /// Obtém o identificador de referência do recurso manipulado.
    /// </summary>
    public string? ResourceId { get; private set; }

    /// <summary>
    /// Obtém detalhes, payload ou justificativa textual associada à operação.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// Obtém um indicador se a ação foi disparada em contexto de Shadow Mode (impersonação ativa).
    /// </summary>
    public bool IsImpersonated { get; private set; }

    /// <summary>
    /// Obtém o identificador do SuperAdmin que executou a operação sob Shadow Mode.
    /// </summary>
    public Guid? SuperAdminId { get; private set; }

    /// <summary>
    /// Obtém o número do ticket de suporte vinculado à sessão de suporte.
    /// </summary>
    public string? SupportTicketId { get; private set; }

    /// <summary>
    /// Obtém o identificador único da sessão de impersonação ativa no momento da operação.
    /// </summary>
    public Guid? ImpersonationSessionId { get; private set; }

    /// <summary>
    /// Obtém o endereço IP de origem que originou a requisição.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Obtém o timestamp UTC exato no qual o registro de auditoria foi gravado.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém a lista imutável de tags semânticas associadas a este evento de auditoria.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Cria e valida uma nova entrada de auditoria imutável no Catálogo Master.
    /// Se a operação for realizada em modo de impersonação, a tag <see cref="MasterAuditTags.PerformedBySuperadmin"/>
    /// é automaticamente anexada e os metadados do operador tornam-se mandatórios.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant afetado, se houver.</param>
    /// <param name="action">Ação executada (obrigatória).</param>
    /// <param name="resource">Tipo de recurso (obrigatório).</param>
    /// <param name="resourceId">Identificador do recurso.</param>
    /// <param name="details">Detalhes adicionais da operação.</param>
    /// <param name="isImpersonated">Flag informando se o contexto estava sob impersonação.</param>
    /// <param name="superAdminId">ID do SuperAdmin responsável (obrigatório se impersonated).</param>
    /// <param name="supportTicketId">Ticket do chamado de suporte (obrigatório se impersonated).</param>
    /// <param name="impersonationSessionId">Identificador da sessão (obrigatório se impersonated).</param>
    /// <param name="ipAddress">Endereço IP da chamada.</param>
    /// <param name="createdAtUtc">Timestamp UTC do evento.</param>
    /// <param name="additionalTags">Tags semânticas complementares opcionais.</param>
    /// <returns>Resultado contendo a entidade ou erro de validação.</returns>
    public static Result<MasterAuditEntry> Record(
        Guid? tenantId,
        string action,
        string resource,
        string? resourceId,
        string? details,
        bool isImpersonated,
        Guid? superAdminId,
        string? supportTicketId,
        Guid? impersonationSessionId,
        string? ipAddress,
        DateTime createdAtUtc,
        IEnumerable<string>? additionalTags = null)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(resource))
        {
            return Result<MasterAuditEntry>.Failure(
                Error.Validation("Audit.InvalidActionOrResource", "Action and Resource must be provided for audit entries."));
        }

        var tagList = new List<string>();
        if (additionalTags is not null)
        {
            tagList.AddRange(additionalTags);
        }

        if (isImpersonated)
        {
            if (!superAdminId.HasValue || superAdminId.Value == Guid.Empty ||
                string.IsNullOrWhiteSpace(supportTicketId) ||
                !impersonationSessionId.HasValue || impersonationSessionId.Value == Guid.Empty)
            {
                return Result<MasterAuditEntry>.Failure(
                    Error.Validation("Audit.ImpersonationMetadataRequired", "SuperAdminId, SupportTicketId and ImpersonationSessionId are mandatory when operation is impersonated."));
            }

            if (!tagList.Contains(MasterAuditTags.PerformedBySuperadmin))
            {
                tagList.Add(MasterAuditTags.PerformedBySuperadmin);
            }
        }

        var entry = new MasterAuditEntry(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            action: action.Trim(),
            resource: resource.Trim(),
            resourceId: resourceId?.Trim(),
            details: details?.Trim(),
            isImpersonated: isImpersonated,
            superAdminId: isImpersonated ? superAdminId : null,
            supportTicketId: isImpersonated ? supportTicketId?.Trim() : null,
            impersonationSessionId: isImpersonated ? impersonationSessionId : null,
            ipAddress: ipAddress?.Trim(),
            createdAtUtc: createdAtUtc,
            tags: tagList);

        return Result<MasterAuditEntry>.Success(entry);
    }
}
