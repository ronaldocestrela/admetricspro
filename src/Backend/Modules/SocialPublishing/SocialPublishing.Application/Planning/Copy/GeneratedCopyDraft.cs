namespace SocialPublishing.Application.Planning.Copy;

/// <summary>
/// Rascunho de cópia retornado pelo provedor de IA antes da validação de domínio.
/// </summary>
/// <param name="Caption">Texto da legenda.</param>
/// <param name="BrandHashtags">Hashtags de marca.</param>
/// <param name="NicheHashtags">Hashtags de nicho.</param>
/// <param name="ReachHashtags">Hashtags de alcance.</param>
public sealed record GeneratedCopyDraft(
    string Caption,
    IReadOnlyList<string> BrandHashtags,
    IReadOnlyList<string> NicheHashtags,
    IReadOnlyList<string> ReachHashtags);
