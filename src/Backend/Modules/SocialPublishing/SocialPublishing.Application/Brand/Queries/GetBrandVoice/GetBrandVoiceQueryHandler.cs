using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Application.Brand.DTOs;
using SocialPublishing.Domain;

namespace SocialPublishing.Application.Brand.Queries.GetBrandVoice;

/// <summary>
/// Manipulador da consulta <see cref="GetBrandVoiceQuery"/>.
/// </summary>
public sealed class GetBrandVoiceQueryHandler : IQueryHandler<GetBrandVoiceQuery, BrandVoiceDto>
{
    private readonly IBrandVoiceRepository _brandVoiceRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetBrandVoiceQueryHandler"/>.
    /// </summary>
    /// <param name="brandVoiceRepository">Repositório de brand voice.</param>
    public GetBrandVoiceQueryHandler(IBrandVoiceRepository brandVoiceRepository)
    {
        _brandVoiceRepository = brandVoiceRepository ?? throw new ArgumentNullException(nameof(brandVoiceRepository));
    }

    /// <inheritdoc />
    public async Task<Result<BrandVoiceDto>> Handle(GetBrandVoiceQuery request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.WorkspaceRequired);
        }

        var profile = await _brandVoiceRepository.GetByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        if (profile is null)
        {
            return Result<BrandVoiceDto>.Failure(SocialErrors.BrandVoiceNotFound);
        }

        return Result<BrandVoiceDto>.Success(BrandVoiceMapping.ToDto(profile));
    }
}
