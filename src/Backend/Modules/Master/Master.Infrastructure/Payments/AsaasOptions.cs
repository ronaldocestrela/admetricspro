namespace Master.Infrastructure.Payments;

/// <summary>
/// Opções de configuração para integração com a API REST do gateway Asaas.
/// </summary>
public sealed class AsaasOptions
{
    /// <summary>
    /// Chave de seção para mapeamento nas configurações da aplicação.
    /// </summary>
    public const string SectionName = "Asaas";

    /// <summary>
    /// Chave de API de autenticação no Asaas (header 'access_token').
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// URL base da API Asaas (ex.: https://sandbox.asaas.com/api/v3 ou https://api.asaas.com/v3).
    /// </summary>
    public string BaseUrl { get; set; } = "https://sandbox.asaas.com/api/v3";

    /// <summary>
    /// Token de autenticação recebido no cabeçalho 'asaas-access-token' para validação de webhooks.
    /// </summary>
    public string WebhookToken { get; set; } = string.Empty;
}
