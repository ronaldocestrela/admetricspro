using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Reports;

/// <summary>
/// Representa um destinatário configurado para receber o relatório automático da agência.
/// </summary>
public sealed class ReportRecipient
{
    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReportRecipient"/> para o EF Core.
    /// </summary>
    private ReportRecipient()
    {
        Name = string.Empty;
        Destination = string.Empty;
    }

    /// <summary>
    /// Construtor privado para fabricação controlada do destinatário.
    /// </summary>
    private ReportRecipient(string name, ReportDeliveryChannel channel, string destination)
    {
        Name = name;
        Channel = channel;
        Destination = destination;
    }

    /// <summary>
    /// Nome do contato ou stakeholder receptor.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Canal de entrega preferencial (E-mail ou WhatsApp).
    /// </summary>
    public ReportDeliveryChannel Channel { get; private set; }

    /// <summary>
    /// Endereço de destino (E-mail ou Telefone internacional).
    /// </summary>
    public string Destination { get; private set; }

    /// <summary>
    /// Cria uma nova instância validada de <see cref="ReportRecipient"/>.
    /// </summary>
    /// <param name="name">Nome do destinatário.</param>
    /// <param name="channel">Canal de entrega (E-mail ou WhatsApp).</param>
    /// <param name="destination">Endereço de e-mail ou telefone formatado.</param>
    /// <returns>Resultado com a entidade ou erro de validação.</returns>
    public static Result<ReportRecipient> Create(string name, ReportDeliveryChannel channel, string destination)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ReportRecipient>.Failure(Error.Validation(
                "ReportRecipient.EmptyName",
                "O nome do destinatário é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            return Result<ReportRecipient>.Failure(Error.Validation(
                "ReportRecipient.EmptyDestination",
                "O destino (e-mail ou telefone) é obrigatório."));
        }

        var trimmedDestination = destination.Trim();

        if (channel == ReportDeliveryChannel.Email)
        {
            if (!trimmedDestination.Contains('@') || !trimmedDestination.Contains('.'))
            {
                return Result<ReportRecipient>.Failure(Error.Validation(
                    "ReportRecipient.InvalidEmail",
                    $"O endereço de e-mail '{trimmedDestination}' é inválido."));
            }
        }
        else if (channel == ReportDeliveryChannel.WhatsApp)
        {
            // Valida se contém dígitos mínimos para telefone internacional
            var digitsOnly = new string(trimmedDestination.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length < 10)
            {
                return Result<ReportRecipient>.Failure(Error.Validation(
                    "ReportRecipient.InvalidPhone",
                    $"O número de WhatsApp '{trimmedDestination}' deve conter pelo menos 10 dígitos com código de área/país."));
            }
        }

        return Result<ReportRecipient>.Success(new ReportRecipient(name.Trim(), channel, trimmedDestination));
    }
}
