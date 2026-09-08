namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Opções de configuração para o serviço de envio de e-mails transacionais.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Nome da seção padrão no arquivo de configuração (appsettings.json).
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Provedor selecionado para despacho ("InMemory", "Logging" ou "Smtp").
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// Endereço do host do servidor SMTP.
    /// </summary>
    public string SmtpHost { get; set; } = "localhost";

    /// <summary>
    /// Porta do servidor SMTP (padrão 587 para TLS).
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Nome de usuário para autenticação no servidor SMTP.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Senha para autenticação no servidor SMTP.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Indica se a conexão segura SSL/TLS deve ser utilizada.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Endereço de e-mail padrão do remetente corporativo.
    /// </summary>
    public string FromEmail { get; set; } = "noreply@admetricspro.com.br";

    /// <summary>
    /// Nome de exibição padrão do remetente corporativo.
    /// </summary>
    public string FromDisplayName { get; set; } = "AdMetricsPro";
}
