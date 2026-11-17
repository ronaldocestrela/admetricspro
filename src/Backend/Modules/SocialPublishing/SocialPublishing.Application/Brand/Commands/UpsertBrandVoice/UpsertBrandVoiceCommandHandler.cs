using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain;
using SocialPublishing.Domain.Brand;

namespace SocialPublishing.Application.Brand.Commands.UpsertBrandVoice;

/// <summary>
/// Persiste ou atualiza o perfil de voz de marca de um workspace.
/// </summary>
public sealed class UpsertBrandVoiceCommandHandler : ICommandHandler<UpsertBrandVoiceCommand, BrandVoiceDto>
{
    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly ISocialPublishingUnitOfWork _unitOfWork;
    private readonly ITenantContextAccessor _tenantContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="UpsertBrandVoiceCommandHandler"/>.
    /// </summary>
    /// <param name="brandVoiceRepository">Repositório de brand voice.</param>
    /// <param name="unitOfWork">Unit of work transacional do módulo.</param>
    /// <param name="tenantContextAccessor">Contexto de tenant da requisição.</param>
    public UpsertBrandVoiceCommandHandler(
        IBrandVoiceRepository brandVoiceRepository,
        ISocialPublishingUnitOfWork unitOfWork,
        ITenantContextAccessor tenantContextAccessor)
    {
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<BrandVoiceDto>> Handle(UpsertBrandVoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (_tenantContextAccessor.TenantContext is null)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.TenantRequired);
        }

        var weightsResult = PillarWeights.Create(
            request.AuthorityWeight,
            request.SaleWeight,
            request.ConnectionWeight,
            request.SocialProofWeight);
        if (weightsResult.IsFailure)
        {
            return Result<BrandVoiceDto>.Failure(weightsResult.Error);
        }

        var existing = await _brandVoiceRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        BrandVoiceProfile profile;

        if (existing is null)
        {
            var createResult = BrandVoiceProfile.Create(
                request.WorkspaceId,
                request.Tone,
                request.Persona,
                request.MandatoryLinks,
                request.ForbiddenTerms,
                request.EmojiPolicy,
                request.HashtagLimit,
                request.AuthorityWeight,
                request.SaleWeight,
                request.ConnectionWeight,
                request.SocialProofWeight,
                request.LanguageCode);

            if (createResult.IsFailure)
            {
                return Result<BrandVoiceDto>.Failure(createResult.Error);
            }

            profile = createResult.Value;
            await _brandVoiceRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            var reviseResult = existing.Revise(
                request.Tone,
                request.Persona,
                request.MandatoryLinks,
                request.ForbiddenTerms,
                request.EmojiPolicy,
                request.HashtagLimit,
                request.AuthorityWeight,
                request.SaleWeight,
                request.ConnectionWeight,
                request.SocialProofWeight,
                request.LanguageCode);

            if (reviseResult.IsFailure)
            {
                return Result<BrandVoiceDto>.Failure(reviseResult.Error);
            }

            profile = existing;
            await _brandVoiceRepository.UpdateAsync(profile, cancellationToken);
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<BrandVoiceDto>.Success(BrandVoiceMapping.ToDto(profile));
    }
}
