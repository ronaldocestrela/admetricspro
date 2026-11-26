using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Brand.Queries.GetBrandVoiceLogo;

/// <summary>
/// Lê a logo do cliente do object storage para prévia na UI.
/// </summary>
public sealed class GetBrandVoiceLogoQueryHandler : IQueryHandler<GetBrandVoiceLogoQuery, BrandVoiceLogoContentDto>
{
    private readonly IBrandVoiceRepository _brandVoiceRepository;
    private readonly IBrandLogoStore _brandLogoStore;

    /// <summary>
    /// Inicializa o manipulador de leitura da logo do cliente.
    /// </summary>
    /// <param name="brandVoiceRepository">Repositório de brand voice.</param>
    /// <param name="brandLogoStore">Armazenamento da logo.</param>
    public GetBrandVoiceLogoQueryHandler(
        IBrandVoiceRepository brandVoiceRepository,
        IBrandLogoStore brandLogoStore)
    {
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
        _brandLogoStore = brandLogoStore ?? throw new ArgumentNullException(nameof(brandLogoStore));
    }

    /// <inheritdoc />
    public async Task<Result<BrandVoiceLogoContentDto>> Handle(
        GetBrandVoiceLogoQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<BrandVoiceLogoContentDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        var profile = await _brandVoiceRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        if (profile is null || !profile.HasLogo)
        {
            return Result<BrandVoiceLogoContentDto>.Failure(SocialErrors.BrandLogoNotFound);
        }

        var open = await _brandLogoStore.OpenReadAsync(profile.LogoObjectKey!, cancellationToken);
        if (open.IsFailure)
        {
            return Result<BrandVoiceLogoContentDto>.Failure(open.Error);
        }

        await using (open.Value)
        {
            using var buffer = new MemoryStream();
            await open.Value.CopyToAsync(buffer, cancellationToken);
            return Result<BrandVoiceLogoContentDto>.Success(
                new BrandVoiceLogoContentDto(
                    profile.LogoContentType!,
                    Convert.ToBase64String(buffer.ToArray())));
        }
    }
}