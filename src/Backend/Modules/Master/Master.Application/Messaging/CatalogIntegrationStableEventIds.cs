using System.Security.Cryptography;
using System.Text;
using Master.Domain.Tenants;

namespace Master.Application.Messaging;

/// <summary>
/// Gera identificadores de evento determinísticos para idempotência de enfileiramento no MasterDb.
/// </summary>
public static class CatalogIntegrationStableEventIds
{
    /// <summary>
    /// EventId estável por tenant e tipo de aviso de trial.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="noticeType">Marco da régua de trial.</param>
    /// <returns>GUID determinístico.</returns>
    public static Guid ForTrialNotice(Guid tenantId, TrialNoticeType noticeType) =>
        CreateDeterministicGuid($"trial:{tenantId:N}:{(int)noticeType}");

    /// <summary>
    /// EventId estável por tenant, estágio de dunning e vencimento da fatura em aberto.
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="stage">Estágio atingido.</param>
    /// <param name="paymentDueDateUtc">Vencimento da cobrança em aberto.</param>
    /// <returns>GUID determinístico.</returns>
    public static Guid ForDunningStage(Guid tenantId, DunningStage stage, DateTime paymentDueDateUtc) =>
        CreateDeterministicGuid($"dunning:{tenantId:N}:{stage}:{paymentDueDateUtc:O}");

    private static Guid CreateDeterministicGuid(string seed)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}