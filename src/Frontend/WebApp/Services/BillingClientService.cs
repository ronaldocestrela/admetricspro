using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Billing.Checkout.Commands.ProcessCheckout;
using Master.Application.Billing.Checkout.Queries.GetCheckoutPreview;
using Master.Application.Billing.Checkout.Queries.GetPaymentStatus;
using Master.Domain.Tenants;

namespace WebApp.Services;

/// <summary>
/// Implementação de cliente HTTP para o módulo financeiro e checkout de assinaturas no Blazor Server.
/// </summary>
public sealed class BillingClientService : IBillingClientService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BillingClientService"/>.
    /// </summary>
    /// <param name="httpClient">Instância de <see cref="HttpClient"/> configurada para a Web API.</param>
    public BillingClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<CheckoutPreviewDto>> GetCheckoutPreviewAsync(
        Guid tenantId,
        SubscriptionTier tier,
        string billingCycle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/v1/billing/checkout/preview?tenantId={tenantId}&tier={tier}&billingCycle={Uri.EscapeDataString(billingCycle)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<CheckoutPreviewDto>>(JsonOptions, cancellationToken);
            return result ?? Result<CheckoutPreviewDto>.Failure(
                Error.Failure("Billing.InvalidResponse", "Resposta vazia ou inválida da Web API de faturamento."));
        }
        catch (Exception ex)
        {
            return Result<CheckoutPreviewDto>.Failure(
                Error.Failure("Billing.NetworkError", $"Falha de comunicação com a Web API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<ProcessCheckoutResult>> ProcessCheckoutAsync(
        ProcessCheckoutCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            return Result<ProcessCheckoutResult>.Failure(
                Error.Validation("Billing.CommandRequired", "Os parâmetros de checkout são obrigatórios."));
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/billing/checkout", command, JsonOptions, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<ProcessCheckoutResult>>(JsonOptions, cancellationToken);
            return result ?? Result<ProcessCheckoutResult>.Failure(
                Error.Failure("Billing.InvalidResponse", "Resposta vazia ou inválida da Web API de checkout."));
        }
        catch (Exception ex)
        {
            return Result<ProcessCheckoutResult>.Failure(
                Error.Failure("Billing.NetworkError", $"Falha de comunicação com a Web API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PaymentStatusDto>> GetPaymentStatusAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/v1/billing/checkout/{transactionId}/status";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<PaymentStatusDto>>(JsonOptions, cancellationToken);
            return result ?? Result<PaymentStatusDto>.Failure(
                Error.Failure("Billing.InvalidResponse", "Resposta vazia ou inválida da Web API ao consultar status."));
        }
        catch (Exception ex)
        {
            return Result<PaymentStatusDto>.Failure(
                Error.Failure("Billing.NetworkError", $"Falha de comunicação com a Web API: {ex.Message}"));
        }
    }
}
