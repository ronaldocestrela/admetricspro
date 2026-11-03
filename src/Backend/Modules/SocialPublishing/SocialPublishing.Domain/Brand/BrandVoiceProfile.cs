using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Brand;

/// <summary>
/// Agregado que persiste o DNA de marca (tom, persona, links, termos proibidos e pesos de pilares) por workspace.
/// </summary>
public sealed class BrandVoiceProfile : AggregateRoot<Guid>
{
    private readonly List<string> _mandatoryLinks = new();
    private readonly List<string> _forbiddenTerms = new();

    private BrandVoiceProfile(
        Guid id,
        Guid workspaceId,
        string tone,
        string persona,
        EmojiPolicy emojiPolicy,
        int hashtagLimit,
        PillarWeights pillarWeights,
        string languageCode,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Tone = tone;
        Persona = persona;
        EmojiPolicy = emojiPolicy;
        HashtagLimit = hashtagLimit;
        PillarWeights = pillarWeights;
        LanguageCode = languageCode;
        CreatedAtUtc = createdAtUtc;
    }

    private BrandVoiceProfile()
        : base(Guid.Empty)
    {
        Tone = string.Empty;
        Persona = string.Empty;
        LanguageCode = "pt-BR";
        PillarWeights = null!;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do workspace.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o tom de voz.
    /// </summary>
    public string Tone { get; private set; }

    /// <summary>
    /// Obtém a persona da marca.
    /// </summary>
    public string Persona { get; private set; }

    /// <summary>
    /// Obtém a política de emojis.
    /// </summary>
    public EmojiPolicy EmojiPolicy { get; private set; }

    /// <summary>
    /// Obtém o teto de hashtags por post configurado no perfil.
    /// </summary>
    public int HashtagLimit { get; private set; }

    /// <summary>
    /// Obtém os pesos dos pilares editoriais.
    /// </summary>
    public PillarWeights PillarWeights { get; private set; }

    /// <summary>
    /// Obtém o código de idioma (ex.: pt-BR).
    /// </summary>
    public string LanguageCode { get; private set; }

    /// <summary>
    /// Obtém links obrigatórios absolutos.
    /// </summary>
    public IReadOnlyList<string> MandatoryLinks => _mandatoryLinks.AsReadOnly();

    /// <summary>
    /// Obtém termos proibidos normalizados.
    /// </summary>
    public IReadOnlyList<string> ForbiddenTerms => _forbiddenTerms.AsReadOnly();

    /// <summary>
    /// Obtém o carimbo UTC de criação.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Cria um perfil de voz de marca para o workspace.
    /// </summary>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="tone">Tom de voz.</param>
    /// <param name="persona">Persona.</param>
    /// <param name="mandatoryLinks">URLs absolutas obrigatórias.</param>
    /// <param name="forbiddenTerms">Termos proibidos.</param>
    /// <param name="emojiPolicy">Política de emoji.</param>
    /// <param name="hashtagLimit">Teto de hashtags no perfil.</param>
    /// <param name="authorityWeight">Peso Authority.</param>
    /// <param name="saleWeight">Peso Sale.</param>
    /// <param name="connectionWeight">Peso Connection.</param>
    /// <param name="socialProofWeight">Peso SocialProof.</param>
    /// <param name="languageCode">Código de idioma.</param>
    /// <returns>Resultado com o perfil ou erro de validação.</returns>
    public static Result<BrandVoiceProfile> Create(
        Guid workspaceId,
        string tone,
        string persona,
        IEnumerable<string> mandatoryLinks,
        IEnumerable<string> forbiddenTerms,
        EmojiPolicy emojiPolicy,
        int hashtagLimit,
        int authorityWeight,
        int saleWeight,
        int connectionWeight,
        int socialProofWeight,
        string languageCode = "pt-BR")
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<BrandVoiceProfile>.Failure(SocialErrors.WorkspaceRequired);
        }

        var weightsResult = PillarWeights.Create(authorityWeight, saleWeight, connectionWeight, socialProofWeight);
        if (weightsResult.IsFailure)
        {
            return Result<BrandVoiceProfile>.Failure(weightsResult.Error);
        }

        var normalized = NormalizeContent(tone, persona, mandatoryLinks, forbiddenTerms, hashtagLimit, languageCode);
        if (normalized.IsFailure)
        {
            return Result<BrandVoiceProfile>.Failure(normalized.Error);
        }

        var content = normalized.Value;
        var profile = new BrandVoiceProfile(
            Guid.NewGuid(),
            workspaceId,
            content.Tone,
            content.Persona,
            emojiPolicy,
            content.HashtagLimit,
            weightsResult.Value,
            content.LanguageCode,
            DateTime.UtcNow);

        profile._mandatoryLinks.AddRange(content.MandatoryLinks);
        profile._forbiddenTerms.AddRange(content.ForbiddenTerms);

        return Result<BrandVoiceProfile>.Success(profile);
    }

    /// <summary>
    /// Atualiza o DNA de marca mantendo identidade e carimbo de criação.
    /// </summary>
    /// <param name="tone">Tom de voz.</param>
    /// <param name="persona">Persona.</param>
    /// <param name="mandatoryLinks">URLs absolutas obrigatórias.</param>
    /// <param name="forbiddenTerms">Termos proibidos.</param>
    /// <param name="emojiPolicy">Política de emoji.</param>
    /// <param name="hashtagLimit">Teto de hashtags no perfil.</param>
    /// <param name="authorityWeight">Peso Authority.</param>
    /// <param name="saleWeight">Peso Sale.</param>
    /// <param name="connectionWeight">Peso Connection.</param>
    /// <param name="socialProofWeight">Peso SocialProof.</param>
    /// <param name="languageCode">Código de idioma.</param>
    /// <returns>Resultado de sucesso ou erro de validação.</returns>
    public Result Revise(
        string tone,
        string persona,
        IEnumerable<string> mandatoryLinks,
        IEnumerable<string> forbiddenTerms,
        EmojiPolicy emojiPolicy,
        int hashtagLimit,
        int authorityWeight,
        int saleWeight,
        int connectionWeight,
        int socialProofWeight,
        string languageCode = "pt-BR")
    {
        var weightsResult = PillarWeights.Create(authorityWeight, saleWeight, connectionWeight, socialProofWeight);
        if (weightsResult.IsFailure)
        {
            return Result.Failure(weightsResult.Error);
        }

        var normalized = NormalizeContent(tone, persona, mandatoryLinks, forbiddenTerms, hashtagLimit, languageCode);
        if (normalized.IsFailure)
        {
            return Result.Failure(normalized.Error);
        }

        var content = normalized.Value;
        Tone = content.Tone;
        Persona = content.Persona;
        EmojiPolicy = emojiPolicy;
        HashtagLimit = content.HashtagLimit;
        PillarWeights = weightsResult.Value;
        LanguageCode = content.LanguageCode;

        _mandatoryLinks.Clear();
        _mandatoryLinks.AddRange(content.MandatoryLinks);
        _forbiddenTerms.Clear();
        _forbiddenTerms.AddRange(content.ForbiddenTerms);

        return Result.Success();
    }

    private static Result<(string Tone, string Persona, List<string> MandatoryLinks, List<string> ForbiddenTerms, int HashtagLimit, string LanguageCode)> NormalizeContent(
        string tone,
        string persona,
        IEnumerable<string> mandatoryLinks,
        IEnumerable<string> forbiddenTerms,
        int hashtagLimit,
        string languageCode)
    {
        if (string.IsNullOrWhiteSpace(tone) || string.IsNullOrWhiteSpace(persona))
        {
            return Result<(string, string, List<string>, List<string>, int, string)>.Failure(
                Error.Validation("Social.BrandVoiceRequired", "Tom e persona são obrigatórios."));
        }

        if (hashtagLimit <= 0)
        {
            return Result<(string, string, List<string>, List<string>, int, string)>.Failure(
                Error.Validation("Social.HashtagLimitInvalid", "O teto de hashtags do perfil deve ser positivo."));
        }

        var links = new List<string>();
        foreach (var link in mandatoryLinks)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return Result<(string, string, List<string>, List<string>, int, string)>.Failure(
                    Error.Validation("Social.MandatoryLinkRequired", "Links obrigatórios não podem ser vazios."));
            }

            if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out _))
            {
                return Result<(string, string, List<string>, List<string>, int, string)>.Failure(
                    Error.Validation("Social.MandatoryLinkInvalid", "Cada link obrigatório deve ser uma URL absoluta."));
            }

            links.Add(link.Trim());
        }

        var terms = forbiddenTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        return Result<(string, string, List<string>, List<string>, int, string)>.Success((
            tone.Trim(),
            persona.Trim(),
            links,
            terms,
            hashtagLimit,
            string.IsNullOrWhiteSpace(languageCode) ? "pt-BR" : languageCode.Trim()));
    }
}
