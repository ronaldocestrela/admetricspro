using Automations.Domain.Rules;
using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Automations.Infrastructure.Persistence;

/// <summary>
/// Implementação concreta de <see cref="IAutomationRuleRepository"/> utilizando <see cref="ITenantDbContextAccessor"/>.
/// </summary>
public sealed class AutomationRuleRepository : IAutomationRuleRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AutomationRuleRepository"/>.
    /// </summary>
    /// <param name="contextAccessor">Acessor de contexto de banco do inquilino.</param>
    public AutomationRuleRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task<Result<AutomationRule>> AddAsync(AutomationRule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result<AutomationRule>.Failure(contextResult.Error);
        }

        await contextResult.Value.AutomationRules.AddAsync(rule, cancellationToken);
        return Result<AutomationRule>.Success(rule);
    }

    /// <inheritdoc />
    public async Task<AutomationRule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return null;
        }

        return await contextResult.Value.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutomationRule>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Array.Empty<AutomationRule>();
        }

        return await contextResult.Value.AutomationRules
            .Where(r => r.WorkspaceId == workspaceId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid ruleId, CancellationToken cancellationToken = default)
    {
        var contextResult = await _contextAccessor.GetDbContextAsync(cancellationToken);
        if (contextResult.IsFailure)
        {
            return Result.Failure(contextResult.Error);
        }

        var rule = await contextResult.Value.AutomationRules
            .FirstOrDefaultAsync(r => r.Id == ruleId, cancellationToken);

        if (rule is null)
        {
            return Result.Failure(Error.NotFound("AutomationRule.NotFound", "Regra de automação não localizada."));
        }

        contextResult.Value.AutomationRules.Remove(rule);
        return Result.Success();
    }
}
