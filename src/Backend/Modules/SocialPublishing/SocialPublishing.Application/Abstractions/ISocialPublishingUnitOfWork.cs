using BuildingBlocks.Application.Persistence;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Coordena commit transacional e publicação de eventos de domínio do bounded context SocialPublishing.
/// </summary>
public interface ISocialPublishingUnitOfWork : IUnitOfWork
{
}
