using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace BuildingBlocks.Infrastructure.Emails;

/// <summary>
/// Implementação de <see cref="ISmtpMailTransport"/> usando MailKit com validação de certificado habilitada.
/// </summary>
public sealed class MailKitSmtpTransport : ISmtpMailTransport
{
    /// <inheritdoc />
    public async Task SendAsync(SmtpMailSendRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Message);

        using var client = new SmtpClient
        {
            Timeout = request.TimeoutSeconds * 1000,
            CheckCertificateRevocation = true
        };

        try
        {
            await client.ConnectAsync(
                request.Host,
                request.Port,
                request.SecureSocket,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.UserName) && !string.IsNullOrWhiteSpace(request.Password))
            {
                await client.AuthenticateAsync(request.UserName, request.Password, cancellationToken);
            }

            await client.SendAsync(request.Message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (AuthenticationException ex)
        {
            throw new SmtpDeliveryException(ex.Message, isTransient: false);
        }
        catch (SmtpCommandException ex) when (IsTransientSmtpStatus(ex))
        {
            throw new SmtpDeliveryException(ex.Message, isTransient: true);
        }
        catch (SmtpProtocolException ex)
        {
            throw new SmtpDeliveryException(ex.Message, isTransient: true);
        }
        catch (IOException ex)
        {
            throw new SmtpDeliveryException(ex.Message, isTransient: true);
        }
        catch (SocketException ex)
        {
            throw new SmtpDeliveryException(ex.Message, isTransient: true);
        }
        catch (TimeoutException ex)
        {
            throw new SmtpDeliveryException(ex.Message, isTransient: true);
        }
        catch (SmtpCommandException ex)
        {
            throw new SmtpDeliveryException(ex.Message, isTransient: false);
        }
    }

    private static bool IsTransientSmtpStatus(SmtpCommandException ex) =>
        ex.StatusCode >= SmtpStatusCode.ServiceNotAvailable && ex.StatusCode < SmtpStatusCode.MailboxUnavailable;
}