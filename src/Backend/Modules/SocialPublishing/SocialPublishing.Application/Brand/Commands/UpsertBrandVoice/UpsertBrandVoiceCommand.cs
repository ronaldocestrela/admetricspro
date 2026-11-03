using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Tenants;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain.Brand;
using Tenants.Application.Rbac.Attributes;

namespace SocialPublishing.Application.Brand.Commands.UpsertBrandVoice;

/// <summary>
/// Comando para criar ou atualizar o DNA de marca de um workspace.
/// </summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="Tone">Tom de voz.</param>
/// <param name="Persona">Persona da marca.</param>
/// <param name="MandatoryLinks">Links absolutos obrigatórios.</param>
/// <param name="ForbiddenTerms">Termos proibidos.</param>
/// <param name="EmojiPolicy">Política de emojis.</param>
/// <param name="HashtagLimit">Teto de hashtags no perfil.</param>
/// <param name="AuthorityWeight">Peso Authority.</param>
/// <param name="SaleWeight">Peso Sale.</param>
/// <param name="ConnectionWeight">Peso Connection.</param>
/// <param name="SocialProofWeight">Peso SocialProof.</param>
/// <param name="LanguageCode">Código de idioma.</param>
[RequireTenantPermission(TenantPermission.SocialCopilotGenerate, WorkspaceIdProperty = nameof(WorkspaceId))]
public sealed record UpsertBrandVoiceCommand(
    Guid WorkspaceId,
    string Tone,
    string Persona,
    IReadOnlyList<string> MandatoryLinks,
    IReadOnlyList<string> ForbiddenTerms,
    EmojiPolicy EmojiPolicy,
    int HashtagLimit,
    int AuthorityWeight,
    int SaleWeight,
    int ConnectionWeight,
    int SocialProofWeight,
    string LanguageCode = "pt-BR") : ICommand<BrandVoiceDto>;
