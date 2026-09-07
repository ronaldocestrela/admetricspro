using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Workspaces.Repositories;

/// <summary>
/// Contrato de repositório explícito para persistência e consulta do agregado <see cref="Workspace"/> no banco dedicado do inquilino.
/// </summary>
public interface IWorkspaceRepository
{
    /// <summary>
    /// Obtém um workspace pelo seu identificador único.
    /// </summary>
    /// <param name="id">Identificador único do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade se encontrada; caso contrário, nulo.</returns>
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os workspaces do inquilino corrente, com filtro opcional de status ativo.
    /// </summary>
    /// <param name="activeOnly">Se verdadeiro, filtra apenas ativos; se falso, apenas inativos; se nulo, retorna todos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista somente-leitura de workspaces.</returns>
    Task<IReadOnlyList<Workspace>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe um workspace cadastrado com o documento fiscal informado.
    /// </summary>
    /// <param name="cnpjOrCpf">Documento fiscal sanitizado.</param>
    /// <param name="excludeId">Identificador opcional de workspace a ser desconsiderado (em atualizações).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Verdadeiro se existir; caso contrário, falso.</returns>
    Task<bool> ExistsByCnpjOrCpfAsync(string cnpjOrCpf, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a contagem total de workspaces atualmente com status ativo no inquilino.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de workspaces ativos.</returns>
    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo workspace ao contexto transacional do inquilino.
    /// </summary>
    /// <param name="workspace">Instância do agregado de workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa assíncrona.</returns>
    Task AddAsync(Workspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca o workspace como modificado no contexto transacional.
    /// </summary>
    /// <param name="workspace">Instância do agregado modificado.</param>
    void Update(Workspace workspace);
}
