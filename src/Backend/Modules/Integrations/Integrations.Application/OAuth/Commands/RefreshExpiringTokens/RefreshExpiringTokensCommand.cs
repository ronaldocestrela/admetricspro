using BuildingBlocks.Application.Messaging;

namespace Integrations.Application.OAuth.Commands.RefreshExpiringTokens;

/// <summary>
/// Comando para renovação preventiva de tokens de acesso próximos à expiração no Token Vault.
/// </summary>
/// <param name="Threshold">Janela de antecedência de expiração (padrão: 72 horas).</param>
public sealed record RefreshExpiringTokensCommand(TimeSpan? Threshold = null) : ICommand<int>;
