using BuildingBlocks.Domain.Tenants;

namespace Tenants.Application.Squads.Repositories;

/// <summary>
/// Contrato de repositório explícito para persistência e consulta do agregado <see cref="Squad"/> no banco dedicado do inquilino.
/// </summary>
public interface ISquadRepository
{
    /// <summary>
    /// Obtém um squad pelo identificador único, incluindo membros e workspaces associados.
    /// </summary>
    /// <param name="id">Identificador único do squad.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>A entidade de squad com relacionamentos ou nulo se não encontrado.</returns>
    Task<Squad?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os squads cadastrados no inquilino com filtro opcional por status ativo.
    /// </summary>
    /// <param name="activeOnly">Filtro opcional para retornar apenas squads ativos.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista somente-leitura de squads com seus membros e workspaces.</returns>
    Task<IReadOnlyList<Squad>> GetAllAsync(bool? activeOnly = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se já existe um squad com o nome informado no inquilino (case-insensitive).
    /// </summary>
    /// <param name="name">Nome do squad.</param>
    /// <param name="excludeId">Identificador opcional de squad a desconsiderar (em atualizações).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Verdadeiro se existir squad conflitante; caso contrário, falso.</returns>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os squads aos quais um usuário especificado está vinculado como membro.
    /// </summary>
    /// <param name="userId">Identificador do usuário do inquilino.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de squads vinculados ao usuário.</returns>
    Task<IReadOnlyList<Squad>> GetSquadsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém todos os squads que possuem o workspace especificado em sua carteira de atendimento.
    /// </summary>
    /// <param name="workspaceId">Identificador do cliente/workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de squads que atendem o cliente.</returns>
    Task<IReadOnlyList<Squad>> GetSquadsByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo squad ao contexto operacional do inquilino.
    /// </summary>
    /// <param name="squad">Instância do agregado de squad.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AddAsync(Squad squad, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca o squad como modificado no contexto transacional.
    /// </summary>
    /// <param name="squad">Instância do agregado modificado.</param>
    void Update(Squad squad);

    /// <summary>
    /// Remove o squad do contexto operacional.
    /// </summary>
    /// <param name="squad">Instância do agregado a remover.</param>
    void Remove(Squad squad);
}
