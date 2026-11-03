using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Publishing;

/// <summary>
/// Entrada de publicação orgânica na borda HTTP; o token chega decifrado no escopo do handler.
/// </summary>
/// <param name="AccessToken">Token de acesso em claro (nunca persistido pelo adaptador).</param>
/// <param name="ExternalAccountId">Identificador da conta na rede (Page ID, IG user ID ou open_id TikTok).</param>
/// <param name="Format">Formato decidido pelo domínio.</param>
/// <param name="Caption">Legenda final.</param>
/// <param name="Media">Itens ordenados por <see cref="OrganicPublishMediaItem.SortOrder"/>.</param>
public sealed record OrganicPublishRequest(
    string AccessToken,
    string ExternalAccountId,
    OrganicPostFormat Format,
    string Caption,
    IReadOnlyList<OrganicPublishMediaItem> Media);
