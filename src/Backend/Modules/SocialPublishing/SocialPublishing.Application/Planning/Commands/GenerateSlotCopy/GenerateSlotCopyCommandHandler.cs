using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Planning.Copy;
using SocialPublishing.Application.Planning.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Planning.Commands.GenerateSlotCopy;

/// <summary>
/// Gera cópia validada para um slot editorial usando Brand Voice e provedor de IA.
/// </summary>
public sealed class GenerateSlotCopyCommandHandler : ICommandHandler<GenerateSlotCopyCommand, GeneratedCopyDto>
{
    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly ISocialCreativeGenerator _creativeGenerator;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GenerateSlotCopyCommandHandler"/>.
    /// </summary>
    /// <param name="brandVoiceRepository">Repositório de Brand Voice.</param>
    /// <param name="creativeGenerator">Porta de geração criativa.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant da requisição.</param>
    public GenerateSlotCopyCommandHandler(
        IBrandVoiceRepository brandVoiceRepository,
        ISocialCreativeGenerator creativeGenerator,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
        _creativeGenerator = creativeGenerator ?? throw new ArgumentNullException(nameof(creativeGenerator));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<GeneratedCopyDto>> Handle(GenerateSlotCopyCommand request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<GeneratedCopyDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (_tenantContextAccessor.TenantContext is null)
        {
            return Result<GeneratedCopyDto>.Failure(SocialErrors.TenantRequired);
        }

        var brandVoice = await _brandVoiceRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        if (brandVoice is null)
        {
            return Result<GeneratedCopyDto>.Failure(SocialErrors.BrandVoiceNotFound);
        }

        var slotInput = new EditorialSlotCopyInput(request.Pillar, request.Platform, request.Format);
        var brief = CopyBriefBuilder.Build(brandVoice, slotInput);

        var generated = await _creativeGenerator.GenerateAsync(brief, cancellationToken);
        if (generated.IsFailure)
        {
            return Result<GeneratedCopyDto>.Failure(generated.Error);
        }

        var accepted = CopyAcceptanceGate.Validate(brandVoice, request.Platform, generated.Value);
        if (accepted.IsFailure)
        {
            return Result<GeneratedCopyDto>.Failure(accepted.Error);
        }

        var (caption, _, warnings) = accepted.Value;

        var dto = new GeneratedCopyDto(
            caption,
            new GeneratedHashtagGroupsDto(
                NormalizeTags(generated.Value.BrandHashtags),
                NormalizeTags(generated.Value.NicheHashtags),
                NormalizeTags(generated.Value.ReachHashtags)),
            request.Platform,
            request.Pillar,
            warnings);

        return Result<GeneratedCopyDto>.Success(dto);
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string> tags) =>
        tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t =>
            {
                var tag = t.Trim();
                return tag.StartsWith('#') ? tag[1..] : tag;
            })
            .ToList();
}
