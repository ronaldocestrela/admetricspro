namespace Tenants.Application.Rbac.Models;

/// <summary>
/// Contexto operacional para avaliação de permissões contextuais do inquilino.
/// Encapsula dados do recurso alvo como identificador de workspace, squad ou orçamentos propostos.
/// </summary>
/// <param name="WorkspaceId">Identificador opcional do workspace de cliente alvo da operação.</param>
/// <param name="SquadId">Identificador opcional do squad alvo da operação.</param>
/// <param name="ProposedBudget">Orçamento ou limite proposto a ser validado contra o teto do gestor de mídia.</param>
/// <param name="TargetUserId">Identificador opcional do usuário alvo da operação (ex: para gestão de membros).</param>
public sealed record PermissionContext(
    Guid? WorkspaceId = null,
    Guid? SquadId = null,
    decimal? ProposedBudget = null,
    Guid? TargetUserId = null)
{
    /// <summary>
    /// Instância estática representando um contexto global sem restrições específicas de entidade.
    /// </summary>
    public static readonly PermissionContext Empty = new();
}
