using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Dunning;
using Master.Domain.Tenants;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;

namespace WebApi.Controllers.v1;

/// <summary>
/// Controlador responsável por operações financeiras, cobrança e régua de inadimplência (Dunning Engine).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class BillingController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BillingController"/>.
    /// </summary>
    /// <param name="sender">Mediador de comandos e consultas.</param>
    public BillingController(ISender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    /// <summary>
    /// Dispara imediatamente um ciclo de avaliação da régua de inadimplência e bloqueio progressivo contra os tenants cadastrados.
    /// </summary>
    /// <param name="request">Parâmetros opcionais de execução contendo data de referência UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Sumário detalhado das avaliações e transições executadas.</returns>
    [HttpPost("dunning/execute")]
    [EndpointSummary("Executa o ciclo da régua de inadimplência e suspensão progressiva (Dunning Engine)")]
    [ProducesResponseType(typeof(Result<DunningExecutionSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<DunningExecutionSummaryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<DunningExecutionSummaryResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<DunningExecutionSummaryResponse>>> ExecuteDunningCycle(
        [FromBody] ExecuteDunningApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new ExecuteDunningCycleCommand(request?.ReferenceDateUtc);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => UnprocessableEntity(Result<DunningExecutionSummaryResponse>.Failure(result.Error)),
                _ => BadRequest(Result<DunningExecutionSummaryResponse>.Failure(result.Error))
            };
        }

        var response = new DunningExecutionSummaryResponse(
            result.Value.EvaluatedCount,
            result.Value.TransitionsCount,
            result.Value.SuspendedCount,
            result.Value.UnchangedCount,
            result.Value.ExecutedAtUtc);

        return Ok(Result<DunningExecutionSummaryResponse>.Success(response));
    }

    /// <summary>
    /// Dispara imediatamente um ciclo de avaliação da régua de trial (notificações aos 7, 3 e 1 dias restantes e expiração).
    /// </summary>
    /// <param name="request">Parâmetros opcionais de execução contendo data de referência UTC.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Sumário detalhado das notificações despachadas no ciclo.</returns>
    [HttpPost("trial-notices/execute")]
    [EndpointSummary("Executa o ciclo da régua de lembretes e notificações de término de trial")]
    [ProducesResponseType(typeof(Result<TrialNoticeExecutionSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TrialNoticeExecutionSummaryResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<TrialNoticeExecutionSummaryResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<TrialNoticeExecutionSummaryResponse>>> ExecuteTrialNoticeCycle(
        [FromBody] ExecuteTrialNoticeApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var command = new Master.Application.Billing.Trial.ExecuteTrialNoticeCycleCommand(request?.ReferenceDateUtc);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => UnprocessableEntity(Result<TrialNoticeExecutionSummaryResponse>.Failure(result.Error)),
                _ => BadRequest(Result<TrialNoticeExecutionSummaryResponse>.Failure(result.Error))
            };
        }

        var response = new TrialNoticeExecutionSummaryResponse(
            result.Value.EvaluatedCount,
            result.Value.SevenDayNoticesSent,
            result.Value.ThreeDayNoticesSent,
            result.Value.OneDayNoticesSent,
            result.Value.ExpiredNoticesSent,
            result.Value.FailuresCount,
            result.Value.TotalNoticesSent,
            result.Value.ExecutedAtUtc);

        return Ok(Result<TrialNoticeExecutionSummaryResponse>.Success(response));
    }

    /// <summary>
    /// Consulta os valores líquidos, percentual de economia e data de renovação para pré-visualização de checkout.
    /// </summary>
    /// <param name="tenantId">Identificador único do inquilino.</param>
    /// <param name="tier">Plano de assinatura pretendido (Starter, Pro ou Enterprise).</param>
    /// <param name="billingCycle">Ciclo de cobrança (Monthly ou Annual).</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Detalhes discriminados de valores e descontos.</returns>
    [HttpGet("checkout/preview")]
    [EndpointSummary("Calcula os valores líquidos e descontos para pré-visualização de checkout")]
    [ProducesResponseType(typeof(Result<CheckoutPreviewApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<CheckoutPreviewApiResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<CheckoutPreviewApiResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<CheckoutPreviewApiResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<CheckoutPreviewApiResponse>>> GetCheckoutPreview(
        [FromQuery] Guid tenantId,
        [FromQuery] SubscriptionTier tier,
        [FromQuery] string billingCycle,
        CancellationToken cancellationToken = default)
    {
        var query = new Master.Application.Billing.Checkout.Queries.GetCheckoutPreview.GetCheckoutPreviewQuery(tenantId, tier, billingCycle);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<CheckoutPreviewApiResponse>.Failure(result.Error)),
                ErrorType.Validation => UnprocessableEntity(Result<CheckoutPreviewApiResponse>.Failure(result.Error)),
                _ => BadRequest(Result<CheckoutPreviewApiResponse>.Failure(result.Error))
            };
        }

        var response = new CheckoutPreviewApiResponse(
            result.Value.Tier,
            result.Value.PlanName,
            result.Value.BillingCycle,
            result.Value.BaseMonthlyPrice,
            result.Value.DiscountPercentage,
            result.Value.TotalPayableNow,
            result.Value.SavingsAmount,
            result.Value.NextRenewalDateUtc);

        return Ok(Result<CheckoutPreviewApiResponse>.Success(response));
    }

    /// <summary>
    /// Processa o checkout financeiro de assinatura (via Cartão de Crédito com aprovação imediata ou Pix com geração de QR Code).
    /// </summary>
    /// <param name="request">Dados de contratação, plano, ciclo e forma de pagamento.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado da transação e ativação.</returns>
    [HttpPost("checkout")]
    [EndpointSummary("Processa o checkout e ativação de plano de assinatura via Cartão de Crédito ou Pix")]
    [ProducesResponseType(typeof(Result<ProcessCheckoutApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<ProcessCheckoutApiResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<ProcessCheckoutApiResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result<ProcessCheckoutApiResponse>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Result<ProcessCheckoutApiResponse>>> ProcessCheckout(
        [FromBody] ProcessCheckoutApiRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new Master.Application.Billing.Checkout.Commands.ProcessCheckout.ProcessCheckoutCommand(
            request.TenantId,
            request.Tier,
            request.BillingCycle,
            request.PaymentMethod,
            request.CardHolderName,
            request.CardNumber,
            request.ExpiryMonth,
            request.ExpiryYear,
            request.Ccv);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<ProcessCheckoutApiResponse>.Failure(result.Error)),
                ErrorType.Validation => UnprocessableEntity(Result<ProcessCheckoutApiResponse>.Failure(result.Error)),
                _ => BadRequest(Result<ProcessCheckoutApiResponse>.Failure(result.Error))
            };
        }

        var response = new ProcessCheckoutApiResponse(
            result.Value.TransactionId,
            result.Value.TenantId,
            result.Value.Tier,
            result.Value.BillingCycle,
            result.Value.PaymentMethod,
            result.Value.Amount,
            result.Value.Status,
            result.Value.IsActivated,
            result.Value.PixQrCode,
            result.Value.PixCopiaECola,
            result.Value.PixExpiresAtUtc,
            result.Value.FailureReason);

        return Ok(Result<ProcessCheckoutApiResponse>.Success(response));
    }

    /// <summary>
    /// Consulta o estado de liquidação de uma transação financeira de pagamento.
    /// </summary>
    /// <param name="transactionId">Identificador único da transação.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Estado da liquidação.</returns>
    [HttpGet("checkout/{transactionId:guid}/status")]
    [EndpointSummary("Consulta o status de liquidação de uma transação de pagamento")]
    [ProducesResponseType(typeof(Result<PaymentStatusApiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<PaymentStatusApiResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<PaymentStatusApiResponse>>> GetPaymentStatus(
        [FromRoute] Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var query = new Master.Application.Billing.Checkout.Queries.GetPaymentStatus.GetPaymentStatusQuery(transactionId);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.NotFound => NotFound(Result<PaymentStatusApiResponse>.Failure(result.Error)),
                _ => BadRequest(Result<PaymentStatusApiResponse>.Failure(result.Error))
            };
        }

        var response = new PaymentStatusApiResponse(
            result.Value.TransactionId,
            result.Value.Status,
            result.Value.IsPaid,
            result.Value.PaidAtUtc);

        return Ok(Result<PaymentStatusApiResponse>.Success(response));
    }

    /// <summary>
    /// Recebe notificações assíncronas (webhooks) de liquidação de pagamento enviadas pelos gateways integrados.
    /// </summary>
    /// <param name="provider">Nome do provedor emissor (ex: asaas).</param>
    /// <param name="asaasToken">Token de autenticação enviado no cabeçalho 'asaas-access-token'.</param>
    /// <param name="payload">Cópia do payload JSON recebido.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <returns>Resultado do processamento da notificação.</returns>
    [HttpPost("webhooks/{provider}")]
    [EndpointSummary("Recebe notificações de webhooks de gateways de pagamento (ex: Asaas)")]
    [ProducesResponseType(typeof(Result<Master.Application.Billing.Checkout.Commands.ProcessWebhook.PaymentWebhookResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Master.Application.Billing.Checkout.Commands.ProcessWebhook.PaymentWebhookResult>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result<Master.Application.Billing.Checkout.Commands.ProcessWebhook.PaymentWebhookResult>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Result<Master.Application.Billing.Checkout.Commands.ProcessWebhook.PaymentWebhookResult>>> ProcessWebhook(
        [FromRoute] string provider,
        [FromHeader(Name = "asaas-access-token")] string? asaasToken,
        [FromBody] System.Text.Json.JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        string eventType = string.Empty;
        string gatewayTransactionId = string.Empty;
        decimal? value = null;

        if (payload.TryGetProperty("event", out var eventProp))
        {
            eventType = eventProp.GetString() ?? string.Empty;
        }

        if (payload.TryGetProperty("payment", out var paymentProp))
        {
            if (paymentProp.TryGetProperty("id", out var idProp))
            {
                gatewayTransactionId = idProp.GetString() ?? string.Empty;
            }

            if (paymentProp.TryGetProperty("value", out var valProp) && valProp.TryGetDecimal(out var decVal))
            {
                value = decVal;
            }
        }

        var command = new Master.Application.Billing.Checkout.Commands.ProcessWebhook.ProcessPaymentWebhookCommand(
            Provider: provider,
            WebhookToken: asaasToken,
            EventType: eventType,
            GatewayTransactionId: gatewayTransactionId,
            Amount: value,
            EventDateUtc: DateTime.UtcNow);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(Result<Master.Application.Billing.Checkout.Commands.ProcessWebhook.PaymentWebhookResult>.Failure(result.Error)),
                ErrorType.NotFound => NotFound(Result<Master.Application.Billing.Checkout.Commands.ProcessWebhook.PaymentWebhookResult>.Failure(result.Error)),
                _ => BadRequest(Result<Master.Application.Billing.Checkout.Commands.ProcessWebhook.PaymentWebhookResult>.Failure(result.Error))
            };
        }

        return Ok(result);
    }
}
