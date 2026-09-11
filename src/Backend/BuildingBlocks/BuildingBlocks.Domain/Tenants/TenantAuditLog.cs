using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Entidade de auditoria imutável persistida no banco de dados dedicado do inquilino.
/// Registra alterações de permissões operacionais, papéis de usuários, governança de squads e ações sensíveis.
/// </summary>
public sealed class TenantAuditLog : Entity<Guid>
{
    private TenantAuditLog(
        Guid id,
        Guid userId,
        string userEmail,
        string action,
        string resource,
        string? resourceId,
        string? details,
        string? ipAddress,
        DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        UserEmail = userEmail;
        Action = action;
        Resource = resource;
        ResourceId = resourceId;
        Details = details;
        IpAddress = ipAddress;
        CreatedAtUtc = createdAtUtc;
    }

    private TenantAuditLog()
        : base(Guid.Empty)
    {
        UserEmail = string.Empty;
        Action = string.Empty;
        Resource = string.Empty;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do colaborador ou operador que disparou a ação auditada.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Obtém o email do usuário no momento da execução da ação.
    /// </summary>
    public string UserEmail { get; private set; }

    /// <summary>
    /// Obtém o identificador textual padronizado da ação operacional (ex: "User.RoleChanged", "Squad.LeaderPromoted").
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// Obtém o nome da entidade ou recurso de domínio impactado (ex: "TenantUser", "Squad", "Workspace").
    /// </summary>
    public string Resource { get; private set; }

    /// <summary>
    /// Obtém o identificador do recurso impactado em formato textual.
    /// </summary>
    public string? ResourceId { get; private set; }

    /// <summary>
    /// Obtém detalhes contextuais ou payload serializado descrevendo o estado anterior e o novo estado.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// Obtém o endereço IP de origem da requisição do operador.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// Obtém o timestamp UTC imutável de criação do registro de auditoria.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova entrada de auditoria imutável no banco do inquilino validando as invariantes de domínio.
    /// </summary>
    /// <param name="id">Identificador único do registro de auditoria.</param>
    /// <param name="userId">Identificador do operador que executou a ação.</param>
    /// <param name="userEmail">Email do operador.</param>
    /// <param name="action">Ação executada (obrigatória).</param>
    /// <param name="resource">Nome do recurso afetado (obrigatório).</param>
    /// <param name="resourceId">Identificador do recurso afetado.</param>
    /// <param name="details">Detalhes adicionais ou descrição das alterações.</param>
    /// <param name="ipAddress">Endereço IP da requisição.</param>
    /// <param name="createdAtUtc">Timestamp UTC de criação (default: DateTime.UtcNow).</param>
    /// <returns>Resultado contendo a entidade instanciada ou erro de validação.</returns>
    public static Result<TenantAuditLog> Create(
        Guid id,
        Guid userId,
        string userEmail,
        string action,
        string resource,
        string? resourceId = null,
        string? details = null,
        string? ipAddress = null,
        DateTime? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<TenantAuditLog>.Failure(
                Error.Validation("TenantAuditLog.InvalidId", "O identificador do log de auditoria não pode ser vazio."));
        }

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return Result<TenantAuditLog>.Failure(
                Error.Validation("TenantAuditLog.InvalidUserEmail", "O email do usuário de auditoria é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return Result<TenantAuditLog>.Failure(
                Error.Validation("TenantAuditLog.InvalidAction", "A ação de auditoria é obrigatória."));
        }

        if (string.IsNullOrWhiteSpace(resource))
        {
            return Result<TenantAuditLog>.Failure(
                Error.Validation("TenantAuditLog.InvalidResource", "O recurso alvo de auditoria é obrigatório."));
        }

        var entry = new TenantAuditLog(
            id,
            userId,
            userEmail.Trim(),
            action.Trim(),
            resource.Trim(),
            resourceId?.Trim(),
            details?.Trim(),
            ipAddress?.Trim(),
            createdAtUtc ?? DateTime.UtcNow);

        return Result<TenantAuditLog>.Success(entry);
    }
}
