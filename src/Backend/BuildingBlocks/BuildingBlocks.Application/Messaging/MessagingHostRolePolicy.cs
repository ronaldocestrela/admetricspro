namespace BuildingBlocks.Application.Messaging;

/// <summary>
/// Regras puras de composição do host por <see cref="MessagingHostRole"/>.
/// </summary>
public static class MessagingHostRolePolicy
{
    /// <summary>
    /// Indica se o processo deve registrar laços temporais e o dispatcher de outbox.
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <returns><see langword="true"/> para <see cref="MessagingHostRole.Scheduler"/> ou <see cref="MessagingHostRole.All"/>.</returns>
    public static bool ShouldRegisterSchedulerWorkers(MessagingHostRole role) =>
        role is MessagingHostRole.Scheduler or MessagingHostRole.All;

    /// <summary>
    /// Indica se o processo deve registrar consumidores MassTransit e filas de recebimento.
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <returns><see langword="true"/> para <see cref="MessagingHostRole.Consumer"/> ou <see cref="MessagingHostRole.All"/>.</returns>
    public static bool ShouldRegisterMassTransitConsumers(MessagingHostRole role) =>
        role is MessagingHostRole.Consumer or MessagingHostRole.All;

    /// <summary>
    /// Indica se o processo deve registrar transporte de publicação (MassTransit ou publicador ativo).
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <param name="messagingEnabled">Feature flag global.</param>
    /// <returns><see langword="true"/> quando mensageria está ligada e o papel publica ou consome via broker.</returns>
    public static bool ShouldRegisterMassTransit(MessagingHostRole role, bool messagingEnabled)
    {
        if (!messagingEnabled)
        {
            return false;
        }

        return role is MessagingHostRole.Scheduler or MessagingHostRole.Consumer or MessagingHostRole.All;
    }

    /// <summary>
    /// Indica se apenas o publicador deve ser registrado (sem consumidores).
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <param name="messagingEnabled">Feature flag global.</param>
    /// <returns><see langword="true"/> apenas para <see cref="MessagingHostRole.Scheduler"/> com mensageria ligada.</returns>
    public static bool IsPublisherOnlyRole(MessagingHostRole role, bool messagingEnabled) =>
        messagingEnabled && role is MessagingHostRole.Scheduler;

    /// <summary>
    /// Indica se o health check AMQP deve ser registrado.
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <param name="messagingEnabled">Feature flag global.</param>
    /// <returns><see langword="true"/> quando o processo depende do broker estar acessível no startup probe.</returns>
    public static bool ShouldRegisterRabbitMqHealthCheck(MessagingHostRole role, bool messagingEnabled) =>
        messagingEnabled && role is not MessagingHostRole.Api;

    /// <summary>
    /// Indica se migrações automáticas do MasterDb devem rodar no startup HTTP deste processo.
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <returns><see langword="true"/> para <see cref="MessagingHostRole.Api"/> ou <see cref="MessagingHostRole.All"/>.</returns>
    public static bool ShouldRunMasterMigrationsOnStartup(MessagingHostRole role) =>
        role is MessagingHostRole.Api or MessagingHostRole.All;

    /// <summary>
    /// Indica se o worker de migração social por tenant deve ser registrado.
    /// </summary>
    /// <param name="role">Papel configurado.</param>
    /// <returns><see langword="true"/> para <see cref="MessagingHostRole.Api"/> ou <see cref="MessagingHostRole.All"/>.</returns>
    public static bool ShouldRegisterSocialTenantMigrationWorker(MessagingHostRole role) =>
        role is MessagingHostRole.Api or MessagingHostRole.All;
}