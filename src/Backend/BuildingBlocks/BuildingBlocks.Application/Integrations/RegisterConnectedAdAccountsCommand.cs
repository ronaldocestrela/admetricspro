using BuildingBlocks.Application.Messaging;

namespace BuildingBlocks.Application.Integrations;

/// <summary>Metadados verificados externamente pelo módulo Integrations.</summary>
/// <param name="ExternalId">ID externo canônico.</param>
/// <param name="Name">Nome externo.</param>
/// <param name="Currency">Moeda.</param>
/// <param name="TimeZone">Fuso.</param>
/// <param name="AccountStatus">Status remoto.</param>
/// <param name="Tasks">Tarefas efetivamente concedidas.</param>
public sealed record VerifiedAdAccount(string ExternalId, string Name, string Currency, string? TimeZone, int? AccountStatus, string Tasks);

/// <summary>Contrato interno para persistir contas já confirmadas na plataforma.</summary>
/// <param name="WorkspaceId">Workspace autorizado.</param>
/// <param name="ConnectionId">Cofre proprietário da autorização.</param>
/// <param name="Accounts">Contas verificadas.</param>
public sealed record RegisterConnectedAdAccountsCommand(Guid WorkspaceId, Guid ConnectionId, IReadOnlyList<VerifiedAdAccount> Accounts) : ICommand<IReadOnlyList<Guid>>;

/// <summary>Desativa vínculos locais sem revogar a autorização global do usuário na Meta.</summary>
/// <param name="WorkspaceId">Workspace proprietário.</param>
/// <param name="AccountId">Conta interna, ou nulo para todas as contas Meta.</param>
public sealed record DisconnectMetaAccountsCommand(Guid WorkspaceId, Guid? AccountId = null) : ICommand;
