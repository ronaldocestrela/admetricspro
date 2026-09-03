namespace WebApi.Models;

/// <summary>
/// Requisição para execução manual de um ciclo de cobrança e dunning.
/// </summary>
/// <param name="ReferenceDateUtc">Data e hora UTC de referência opcional para simulação ou teste de transições.</param>
public sealed record ExecuteDunningApiRequest(DateTime? ReferenceDateUtc = null);

/// <summary>
/// Resposta com métricas e sumário de execução de um ciclo do motor de dunning.
/// </summary>
/// <param name="EvaluatedCount">Total de tenants avaliados no ciclo.</param>
/// <param name="TransitionsCount">Total de tenants que transitaram de estágio na régua.</param>
/// <param name="SuspendedCount">Total de tenants suspensos por atingirem D+14.</param>
/// <param name="UnchangedCount">Total de tenants sem alteração de estágio.</param>
/// <param name="ExecutedAtUtc">Timestamp UTC de execução do ciclo.</param>
public sealed record DunningExecutionSummaryResponse(
    int EvaluatedCount,
    int TransitionsCount,
    int SuspendedCount,
    int UnchangedCount,
    DateTime ExecutedAtUtc);
