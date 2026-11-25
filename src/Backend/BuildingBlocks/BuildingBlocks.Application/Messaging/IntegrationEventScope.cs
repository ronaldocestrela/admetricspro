namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Escopo de persistência do evento de integração (catálogo no Master ou operacional no tenant).
/// </summary>
public enum IntegrationEventScope
{
    /// <summary>
    /// Evento de catálogo no <c>MasterDb</c>; <c>TenantId</c> é opcional.
    /// </summary>
    Catalog = 0,

    /// <summary>
    /// Evento operacional no banco do tenant; <c>TenantId</c> é obrigatório.
    /// </summary>
    Operational = 1,
}