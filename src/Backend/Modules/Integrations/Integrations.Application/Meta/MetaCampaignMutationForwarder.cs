using System.Text.Json;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.Campaigns;
using MediatR;

namespace Integrations.Application.Meta;

internal static class MetaCampaignMutationForwarder
{
    internal static async Task<Result> ForwardAsync(Campaign campaign, ICampaignHierarchyRepository accounts, ISender sender,
        CancellationToken cancellationToken, string? status = null, decimal? budget = null)
    {
        if (campaign.Platform != "MetaAds") return Result.Success();
        var linked = await accounts.GetAccountsForSyncAsync(campaign.WorkspaceId, campaign.ConnectedAdAccountId, "MetaAds", cancellationToken);
        var account = linked.FirstOrDefault(a => a.Id == campaign.ConnectedAdAccountId && a.WorkspaceId == campaign.WorkspaceId && a.Status == "Connected");
        if (account is null) return Result.Failure(Error.NotFound("Meta.AccountNotLinked", "Conta Meta não vinculada ativamente ao workspace."));
        if (account.IsDemo) return Result.Success();
        if (budget.HasValue && (campaign.Currency is not ("BRL" or "USD" or "EUR") || budget.Value > long.MaxValue / 100m || budget.Value * 100m != decimal.Truncate(budget.Value * 100m)))
            return Result.Failure(Error.Validation("Meta.UnsupportedBudgetPrecision", "O reajuste monetário integrado suporta BRL, USD e EUR com até duas casas decimais."));
        var data = status is not null ? JsonSerializer.SerializeToElement(new { status }) :
            JsonSerializer.SerializeToElement(new { daily_budget = (budget!.Value * 100m).ToString("0", System.Globalization.CultureInfo.InvariantCulture) });
        var result = await sender.Send(new WriteMetaResourceCommand(Guid.NewGuid(), campaign.WorkspaceId, campaign.ConnectedAdAccountId,
            "campaigns", campaign.ExternalCampaignId, data, ConfirmActivation: status == "ACTIVE"), cancellationToken);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}
