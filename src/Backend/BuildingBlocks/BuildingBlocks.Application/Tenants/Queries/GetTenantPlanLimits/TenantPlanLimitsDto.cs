namespace BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;

/// <summary>
/// DTO seguro contendo as cotas operacionais e financeiras vinculadas ao plano contratado pelo inquilino.
/// </summary>
/// <param name="Tier">Nome da classificação comercial do tier (ex.: Starter, Pro, Enterprise, Trial).</param>
/// <param name="MaxWorkspaces">Cota máxima permitida de workspaces (clientes) ativos.</param>
/// <param name="MaxSeats">Cota máxima de usuários/assentos na agência.</param>
/// <param name="MonthlyAdSpendCap">Teto máximo mensal consolidado de investimento em mídia paga.</param>
public sealed record TenantPlanLimitsDto(
    string Tier,
    int MaxWorkspaces,
    int MaxSeats,
    decimal MonthlyAdSpendCap);
