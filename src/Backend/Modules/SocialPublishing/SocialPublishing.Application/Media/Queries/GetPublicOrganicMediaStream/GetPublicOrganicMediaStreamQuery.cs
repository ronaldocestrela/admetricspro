using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Media.Queries.GetPublicOrganicMediaStream;

/// <summary>
/// Consulta de stream de mídia orgânica via token HMAC público.
/// </summary>
/// <param name="Token">Token opaco da URL.</param>
public sealed record GetPublicOrganicMediaStreamQuery(string Token) : IQuery<PublicOrganicMediaStreamResult>;

/// <summary>
/// Conteúdo pronto para resposta HTTP de stream.
/// </summary>
/// <param name="Content">Fluxo legível do objeto.</param>
/// <param name="ContentType">Tipo MIME do cabeçalho HTTP.</param>
public sealed record PublicOrganicMediaStreamResult(Stream Content, string ContentType);