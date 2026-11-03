using SocialPublishing.Domain.Brand;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Repositório de persistência do agregado <see cref="BrandVoiceProfile"/> no banco do tenant.
/// </summary>
public interface IBrandVoiceRepository
{
    /// <summary>
    /// Obtém um perfil de voz de marca pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do perfil.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O perfil ou <c>null</c> se não existir.</returns>
    Task<BrandVoiceProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista perfis de voz de marca de um workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Lista de perfis do workspace.</returns>
    Task<IReadOnlyList<BrandVoiceProfile>> ListByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo perfil de voz de marca ao contexto.
    /// </summary>
    /// <param name="profile">Agregado a persistir.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task AddAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca um perfil existente como modificado no contexto.
    /// </summary>
    /// <param name="profile">Agregado atualizado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Tarefa de conclusão.</returns>
    Task UpdateAsync(BrandVoiceProfile profile, CancellationToken cancellationToken = default);
}
