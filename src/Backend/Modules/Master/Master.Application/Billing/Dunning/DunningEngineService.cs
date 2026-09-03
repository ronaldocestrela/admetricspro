using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Tenants;
using Master.Domain.Tenants.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Master.Application.Billing.Dunning;

/// <summary>
/// Orchestrates the automated evaluation of tenant overdue payments and executes progressive suspension policies.
/// </summary>
public sealed class DunningEngineService : IDunningEngineService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<DunningEngineService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DunningEngineService"/> class.
    /// </summary>
    /// <param name="tenantRepository">Tenant persistence repository.</param>
    /// <param name="unitOfWork">Unit of work for committing status updates.</param>
    /// <param name="publisher">In-memory mediator publisher for domain events.</param>
    /// <param name="logger">Structured logger instance.</param>
    public DunningEngineService(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<DunningEngineService> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<DunningExecutionSummary>> ProcessDunningCycleAsync(
        DateTime? referenceDateUtc = null,
        CancellationToken cancellationToken = default)
    {
        var referenceUtc = referenceDateUtc ?? DateTime.UtcNow;
        _logger.LogInformation("Starting dunning evaluation cycle with reference timestamp {ReferenceUtc}", referenceUtc);

        var tenants = await _tenantRepository.GetTenantsForDunningEvaluationAsync(cancellationToken);

        int evaluatedCount = tenants.Count;
        int transitionsCount = 0;
        int suspendedCount = 0;
        int unchangedCount = 0;

        var eventsToDispatch = new List<TenantGracePeriodExceededEvent>();

        foreach (var tenant in tenants)
        {
            var previousStage = tenant.DunningStage;
            var previousStatus = tenant.Status;

            tenant.EvaluateDunningStage(referenceUtc);

            if (tenant.DunningStage != previousStage)
            {
                transitionsCount++;
                _logger.LogInformation(
                    "Tenant {TenantId} ({CompanyName}) transitioned from dunning stage {PreviousStage} to {CurrentStage}",
                    tenant.Id.Value,
                    tenant.CompanyName,
                    previousStage,
                    tenant.DunningStage);
            }
            else
            {
                unchangedCount++;
            }

            if (tenant.Status == TenantStatus.Suspended && previousStatus != TenantStatus.Suspended)
            {
                suspendedCount++;
                _logger.LogWarning(
                    "Tenant {TenantId} ({CompanyName}) has reached maximum overdue threshold and was suspended",
                    tenant.Id.Value,
                    tenant.CompanyName);
            }

            // Extract and queue domain events
            foreach (var domainEvent in tenant.DomainEvents)
            {
                if (domainEvent is TenantGracePeriodExceededEvent gracePeriodEvent)
                {
                    eventsToDispatch.Add(gracePeriodEvent);
                }
            }

            tenant.ClearDomainEvents();
        }

        if (transitionsCount > 0 || suspendedCount > 0)
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }

        // Dispatch collected domain events through in-memory publisher
        foreach (var domainEvent in eventsToDispatch)
        {
            await _publisher.Publish(new DomainEventNotification<TenantGracePeriodExceededEvent>(domainEvent), cancellationToken);
        }

        var summary = new DunningExecutionSummary(
            evaluatedCount,
            transitionsCount,
            suspendedCount,
            unchangedCount,
            referenceUtc);

        _logger.LogInformation(
            "Dunning evaluation cycle finished. Evaluated: {Evaluated}, Transitions: {Transitions}, Suspended: {Suspended}, Unchanged: {Unchanged}",
            evaluatedCount,
            transitionsCount,
            suspendedCount,
            unchangedCount);

        return Result<DunningExecutionSummary>.Success(summary);
    }
}
