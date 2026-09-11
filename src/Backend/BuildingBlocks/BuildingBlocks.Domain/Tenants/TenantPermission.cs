namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Catálogo canônico de permissões operacionais e de governança para inquilinos no AdMetricsPro.
/// Define os privilégios granulares avaliados pelo sistema de controle de acesso baseado em funções (RBAC).
/// </summary>
public enum TenantPermission
{
    /// <summary>
    /// Gerenciamento de assinaturas, planos e faturamento exclusivo da agência (restrito a Owner).
    /// </summary>
    ManageBilling = 1,

    /// <summary>
    /// Gerenciamento de configurações globais do inquilino e integrações da agência.
    /// </summary>
    ManageSettings = 2,

    /// <summary>
    /// Consulta à trilha de auditoria operacional imutável do inquilino.
    /// </summary>
    ViewAuditLogs = 3,

    /// <summary>
    /// Gestão e convite de membros e operadores da agência.
    /// </summary>
    ManageTeamMembers = 4,

    /// <summary>
    /// Criação, alteração e alocação de squads internos de trabalho.
    /// </summary>
    ManageSquads = 5,

    /// <summary>
    /// Gestão de clientes gerenciados (Workspaces).
    /// </summary>
    ManageWorkspaces = 6,

    /// <summary>
    /// Visualização de campanhas, conjuntos e anúncios publicitários.
    /// </summary>
    ViewCampaigns = 7,

    /// <summary>
    /// Criação, edição e ajuste de orçamentos de campanhas publicitárias.
    /// </summary>
    EditCampaigns = 8,

    /// <summary>
    /// Configuração e ativação de regras de automação e travas de segurança.
    /// </summary>
    ManageAutomations = 9,

    /// <summary>
    /// Visualização de margens financeiras, markups e fees de agência (estritamente vedado a clientes finais/guests).
    /// </summary>
    ViewFinancialMargins = 10,

    /// <summary>
    /// Exportação de relatórios analíticos de performance.
    /// </summary>
    ExportReports = 11
}
