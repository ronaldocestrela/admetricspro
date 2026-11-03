using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;
using SocialPublishing.Domain.Connections;
using SocialPublishing.Domain.Posts;

namespace SocialPublishing.Application.Planning.Copy;

/// <summary>
/// Valida rascunhos de IA contra regras de domínio antes de expor a cópia.
/// </summary>
public static class CopyAcceptanceGate
{
    /// <summary>
    /// Valida legenda e hashtags e produz avisos não bloqueantes.
    /// </summary>
    /// <param name="brandVoice">Perfil de marca.</param>
    /// <param name="platform">Rede de destino.</param>
    /// <param name="draft">Rascunho retornado pelo provedor.</param>
    /// <returns>Legenda e hashtags aceitas ou erro de validação.</returns>
    public static Result<(string Caption, HashtagGroup Hashtags, IReadOnlyList<string> Warnings)> Validate(
        BrandVoiceProfile brandVoice,
        OrganicPlatform platform,
        GeneratedCopyDraft draft)
    {
        var caption = draft.Caption?.Trim() ?? string.Empty;

        foreach (var term in brandVoice.ForbiddenTerms)
        {
            if (caption.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return Result<(string, HashtagGroup, IReadOnlyList<string>)>.Failure(SocialErrors.CopyContainsForbiddenTerm);
            }
        }

        var max = CaptionLimits.GetMaxLength(platform);
        if (caption.Length > max)
        {
            return Result<(string, HashtagGroup, IReadOnlyList<string>)>.Failure(SocialErrors.CaptionTooLong);
        }

        var allTags = draft.BrandHashtags
            .Concat(draft.NicheHashtags)
            .Concat(draft.ReachHashtags);

        var groupResult = HashtagGroup.Create(platform, brandVoice.HashtagLimit, allTags);
        if (groupResult.IsFailure)
        {
            return Result<(string, HashtagGroup, IReadOnlyList<string>)>.Failure(groupResult.Error);
        }

        var warnings = new List<string>();
        foreach (var link in brandVoice.MandatoryLinks)
        {
            if (!caption.Contains(link, StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"Link obrigatório ausente na legenda: {link}");
            }
        }

        return Result<(string, HashtagGroup, IReadOnlyList<string>)>.Success((caption, groupResult.Value, warnings));
    }
}
