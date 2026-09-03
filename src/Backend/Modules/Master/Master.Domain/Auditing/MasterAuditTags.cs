namespace Master.Domain.Auditing;

/// <summary>
/// Constantes padronizadas para classificação e filtragem de trilhas de auditoria globais no Catálogo Master.
/// </summary>
public static class MasterAuditTags
{
    /// <summary>
    /// Tag obrigatória atribuída a qualquer ação ou mutação de dados realizada sob o contexto de Shadow Mode.
    /// </summary>
    public const string PerformedBySuperadmin = "performed_by_superadmin";

    /// <summary>
    /// Tag para operações financeiras e de ciclo de vida (bloqueios, dunning, migração de plano).
    /// </summary>
    public const string BillingLifecycle = "billing_lifecycle";

    /// <summary>
    /// Tag para intervenções manuais de suporte e atendimento ao cliente.
    /// </summary>
    public const string SupportIntervention = "support_intervention";
}
