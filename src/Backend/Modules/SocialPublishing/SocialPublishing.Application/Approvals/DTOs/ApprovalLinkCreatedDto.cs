namespace SocialPublishing.Application.Approvals.DTOs;

/// <summary>
/// Resposta única da emissão de link de aprovação (token em claro só neste payload).
/// </summary>
/// <param name="PlainToken">Token opaco Base64Url de 32 bytes.</param>
/// <param name="ExpiresAtUtc">Expiração do link em UTC.</param>
/// <param name="RelativePath">Caminho relativo sugerido (<c>/approve/{token}</c>).</param>
public sealed record ApprovalLinkCreatedDto(
    string PlainToken,
    DateTime ExpiresAtUtc,
    string RelativePath);
