namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Modo de conexão segura SMTP configurável via <see cref="EmailOptions"/>.
/// </summary>
public enum EmailSecureSocketMode
{
    /// <summary>
    /// Negociação automática conforme capacidade do servidor.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Sem TLS (apenas ambientes de relay local confiáveis).
    /// </summary>
    None = 1,

    /// <summary>
    /// STARTTLS após conexão em texto claro (porta 587 típica).
    /// </summary>
    StartTls = 2,

    /// <summary>
    /// SSL/TLS imediato na conexão (porta 465 típica).
    /// </summary>
    SslOnConnect = 3
}