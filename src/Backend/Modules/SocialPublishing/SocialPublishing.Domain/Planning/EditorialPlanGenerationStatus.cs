namespace SocialPublishing.Domain.Planning;

/// <summary>
/// Status da materialização de um plano editorial idempotente.
/// </summary>
public enum EditorialPlanGenerationStatus
{
    /// <summary>
    /// Geração em andamento; requisições concorrentes com a mesma chave devem aguardar ou falhar.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Plano materializado; snapshot pronto para replay idempotente.
    /// </summary>
    Completed = 2,
}
