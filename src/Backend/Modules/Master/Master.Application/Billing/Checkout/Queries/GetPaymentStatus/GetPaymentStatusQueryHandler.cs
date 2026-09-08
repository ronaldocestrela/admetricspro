using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Repositories;
using Master.Domain.Billing;

namespace Master.Application.Billing.Checkout.Queries.GetPaymentStatus;

/// <summary>
/// Manipulador responsável por consultar o estado atual de liquidação de uma transação.
/// </summary>
public sealed class GetPaymentStatusQueryHandler : IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    private readonly ITenantPaymentTransactionRepository _transactionRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetPaymentStatusQueryHandler"/>.
    /// </summary>
    /// <param name="transactionRepository">Repositório de transações financeiras.</param>
    public GetPaymentStatusQueryHandler(ITenantPaymentTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentStatusDto>> Handle(GetPaymentStatusQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
        {
            return Result<PaymentStatusDto>.Failure(
                Error.NotFound("PaymentTransaction.NotFound", "Transação não localizada no catálogo Master."));
        }

        var dto = new PaymentStatusDto(
            transaction.Id,
            transaction.Status,
            IsPaid: transaction.Status == PaymentTransactionStatus.Paid,
            PaidAtUtc: transaction.PaidAtUtc);

        return Result<PaymentStatusDto>.Success(dto);
    }
}
