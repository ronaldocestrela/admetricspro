using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Post que atingiu o limiar de tração, porém não gera novo hit por regra de negócio (ex.: sugestão já aberta).
/// </summary>
/// <param name="OrganicPostId">Identificador do post.</param>
/// <param name="Error">Erro estável associado ao motivo do descarte.</param>
public sealed record OrganicTractionSkippedPost(Guid OrganicPostId, Error Error);
