namespace Tenants.Application.Ftux.DTOs;

/// <summary>
/// Objeto de transferência com o estado consolidado de prontidão e progresso do onboarding operacional (FTUX) da agência.
/// </summary>
/// <param name="IsProvisioned">Indica se a conta e o banco dedicado foram provisionados (Passo 1).</param>
/// <param name="WorkspacesCount">Quantidade de clientes/workspaces cadastrados no inquilino.</param>
/// <param name="ConnectedAdAccountsCount">Quantidade de contas de anúncios conectadas (incluindo contas em modo demonstração).</param>
/// <param name="TeamMembersCount">Quantidade de colaboradores ativos cadastrados no inquilino.</param>
/// <param name="SquadsCount">Quantidade de squads ativos configurados na agência.</param>
/// <param name="Step1Completed">Indica se a etapa 1 (Provisionamento) está concluída.</param>
/// <param name="Step2Completed">Indica se a etapa 2 (1º Workspace) está concluída.</param>
/// <param name="Step3Completed">Indica se a etapa 3 (1ª Conexão de Anúncios) está concluída.</param>
/// <param name="Step4Completed">Indica se a etapa 4 (Equipe ou Squad) está concluída.</param>
/// <param name="ProgressPercentage">Porcentagem consolidada de conclusão do checklist (25%, 50%, 75%, 100%).</param>
/// <param name="IsCompleted">Indica se todas as 4 etapas essenciais do FTUX foram concluídas (100%).</param>
public sealed record TenantFtuxStatusDto(
    bool IsProvisioned,
    int WorkspacesCount,
    int ConnectedAdAccountsCount,
    int TeamMembersCount,
    int SquadsCount,
    bool Step1Completed,
    bool Step2Completed,
    bool Step3Completed,
    bool Step4Completed,
    int ProgressPercentage,
    bool IsCompleted);
