using BuildingBlocks.Application.Messaging;

namespace Tenants.Application.Integrations.Commands.ConnectDemoAdAccount;

/// <summary>
/// Comando para conectar uma conta de anúncios em modo demonstração para aceleração do FTUX.
/// </summary>
/// <param name="WorkspaceId">Identificador do workspace que receberá a conta demonstrativa.</param>
/// <param name="Platform">Plataforma de anúncios (padrão: MetaAds).</param>
public sealed record ConnectDemoAdAccountCommand(
    Guid WorkspaceId,
    string Platform = "MetaAds") : ICommand<Guid>;
