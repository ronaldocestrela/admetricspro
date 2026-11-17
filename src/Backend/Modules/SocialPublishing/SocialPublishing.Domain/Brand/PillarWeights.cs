using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Brand;

/// <summary>
/// Value object com os pesos percentuais dos quatro pilares editoriais.
/// </summary>
public sealed class PillarWeights
{
    private PillarWeights(int authority, int sale, int connection, int socialProof)
    {
        Authority = authority;
        Sale = sale;
        Connection = connection;
        SocialProof = socialProof;
    }

    /// <summary>
    /// Obtém o peso do pilar Authority (0–100).
    /// </summary>
    public int Authority { get; }

    /// <summary>
    /// Obtém o peso do pilar Sale (0–100).
    /// </summary>
    public int Sale { get; }

    /// <summary>
    /// Obtém o peso do pilar Connection (0–100).
    /// </summary>
    public int Connection { get; }

    /// <summary>
    /// Obtém o peso do pilar SocialProof (0–100).
    /// </summary>
    public int SocialProof { get; }

    /// <summary>
    /// Cria pesos válidos que somam exatamente 100.
    /// </summary>
    /// <param name="authority">Peso Authority.</param>
    /// <param name="sale">Peso Sale.</param>
    /// <param name="connection">Peso Connection.</param>
    /// <param name="socialProof">Peso SocialProof.</param>
    /// <returns>Resultado com os pesos ou erro de validação.</returns>
    public static Result<PillarWeights> Create(int authority, int sale, int connection, int socialProof)
    {
        if (authority < 0 || sale < 0 || connection < 0 || socialProof < 0)
        {
            return Result<PillarWeights>.Failure(SocialErrors.PillarWeightsInvalid);
        }

        if (authority + sale + connection + socialProof != 100)
        {
            return Result<PillarWeights>.Failure(SocialErrors.PillarWeightsInvalid);
        }

        return Result<PillarWeights>.Success(new PillarWeights(authority, sale, connection, socialProof));
    }
}
