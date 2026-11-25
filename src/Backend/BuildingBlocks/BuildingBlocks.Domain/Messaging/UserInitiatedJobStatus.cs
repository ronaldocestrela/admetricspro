namespace BuildingBlocks.Domain.Messaging;

/// <summary>
/// Status de execução de um job iniciado por requisição HTTP do usuário.
/// </summary>
public enum UserInitiatedJobStatus
{
    /// <summary>
    /// Job aceito e aguardando consumidor ou ainda não publicado no broker.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Consumidor reivindicou o trabalho.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Trabalho concluído com sucesso.
    /// </summary>
    Succeeded = 3,

    /// <summary>
    /// Falha de negócio permanente registrada no job.
    /// </summary>
    Failed = 4,
}