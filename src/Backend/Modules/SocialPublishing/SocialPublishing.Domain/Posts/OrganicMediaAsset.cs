using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Entidade filha que descreve um item de mídia associado a um post orgânico (metadados de dimensão e ordem).
/// </summary>
public sealed class OrganicMediaAsset : Entity<Guid>
{
    private OrganicMediaAsset(Guid id, int width, int height, int sortOrder)
        : base(id)
    {
        Width = width;
        Height = height;
        SortOrder = sortOrder;
    }

    private OrganicMediaAsset()
        : base(Guid.Empty)
    {
    }

    /// <summary>
    /// Obtém a largura em pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Obtém a altura em pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Obtém a ordem do item no carrossel ou zero para mídia única.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Cria um item de mídia com dimensões validadas.
    /// </summary>
    /// <param name="width">Largura em pixels.</param>
    /// <param name="height">Altura em pixels.</param>
    /// <param name="sortOrder">Ordem no carrossel.</param>
    /// <returns>Resultado com o ativo ou erro de validação.</returns>
    public static Result<OrganicMediaAsset> Create(int width, int height, int sortOrder = 0)
    {
        var aspectResult = AspectRatio.From(width, height);
        if (aspectResult.IsFailure)
        {
            return Result<OrganicMediaAsset>.Failure(aspectResult.Error);
        }

        return Result<OrganicMediaAsset>.Success(new OrganicMediaAsset(Guid.NewGuid(), width, height, sortOrder));
    }

    /// <summary>
    /// Obtém a proporção derivada das dimensões.
    /// </summary>
    /// <returns>Proporção largura/altura.</returns>
    public Result<AspectRatio> GetAspectRatio() => AspectRatio.From(Width, Height);
}
