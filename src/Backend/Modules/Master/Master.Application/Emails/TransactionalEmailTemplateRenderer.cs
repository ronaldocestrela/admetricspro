using BuildingBlocks.Application.Emails;
using Master.Domain.Tenants;

namespace Master.Application.Emails;

/// <summary>
/// Implementação padrão dos templates de e-mails transacionais com design moderno, responsivo e suporte a White-Label.
/// </summary>
public sealed class TransactionalEmailTemplateRenderer : ITransactionalEmailTemplateRenderer
{
    private const string AppBrandName = "AdMetricsPro";
    private const string SupportEmail = "suporte@admetricspro.com.br";

    /// <inheritdoc />
    public EmailMessage RenderWelcomeEmail(
        string recipientEmail,
        string recipientName,
        string companyName,
        string subdomain,
        string? customDomain,
        SubscriptionTier tier)
    {
        var loginUrl = BuildLoginUrl(subdomain, customDomain);
        var subject = $"Bem-vindo ao {AppBrandName}! Seu ambiente exclusivo está pronto";

        var html = $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <title>{{subject}}</title>
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #0b0f19; color: #f1f5f9; margin: 0; padding: 0; }
                .wrapper { max-width: 600px; margin: 40px auto; background-color: #131b2e; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.5); }
                .header { background: linear-gradient(135deg, #4f46e5 0%, #3730a3 100%); padding: 32px; text-align: center; }
                .header h1 { margin: 0; font-size: 24px; font-weight: 700; color: #ffffff; letter-spacing: -0.5px; }
                .header p { margin: 8px 0 0; color: #c7d2fe; font-size: 14px; }
                .content { padding: 36px 32px; }
                .content p { font-size: 15px; line-height: 1.6; color: #94a3b8; margin: 0 0 18px; }
                .content strong { color: #ffffff; }
                .box-details { background-color: #0f172a; border: 1px solid #1e293b; border-radius: 8px; padding: 20px; margin: 24px 0; }
                .box-item { display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 14px; }
                .box-item:last-child { margin-bottom: 0; }
                .box-label { color: #64748b; }
                .box-value { color: #e2e8f0; font-weight: 600; }
                .btn-container { text-align: center; margin: 32px 0 16px; }
                .btn { display: inline-block; background-color: #4f46e5; color: #ffffff !important; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-weight: 600; font-size: 15px; transition: background-color 0.2s ease; box-shadow: 0 4px 12px rgba(79, 70, 229, 0.35); }
                .footer { border-top: 1px solid #1e293b; padding: 24px 32px; text-align: center; font-size: 12px; color: #64748b; background-color: #0b0f19; }
                .footer a { color: #818cf8; text-decoration: none; }
              </style>
            </head>
            <body>
              <div class="wrapper">
                <div class="header">
                  <h1>{{AppBrandName}}</h1>
                  <p>Gestão Unificada de Tráfego Pago Multitenant</p>
                </div>
                <div class="content">
                  <p>Olá, <strong>{{recipientName}}</strong>!</p>
                  <p>É um prazer tê-lo conosco. O ambiente exclusivo da <strong>{{companyName}}</strong> foi provisionado com sucesso e está pronto para receber suas contas de anúncios (Meta, Google, Bing e TikTok Ads).</p>
                  
                  <div class="box-details">
                    <div class="box-item">
                      <span class="box-label">Empresa:</span>
                      <span class="box-value">{{companyName}}</span>
                    </div>
                    <div class="box-item">
                      <span class="box-label">Subdomínio de Acesso:</span>
                      <span class="box-value">{{subdomain}}.admetricspro.com.br</span>
                    </div>
                    <div class="box-item">
                      <span class="box-label">E-mail de Login:</span>
                      <span class="box-value">{{recipientEmail}}</span>
                    </div>
                    <div class="box-item">
                      <span class="box-label">Plano Inicial:</span>
                      <span class="box-value">{{tier}} (14 dias de Trial gratuito)</span>
                    </div>
                  </div>

                  <div class="btn-container">
                    <a href="{{loginUrl}}" class="btn" target="_blank">Acessar Meu Painel Agora</a>
                  </div>

                  <p style="font-size: 13px; color: #64748b; margin-top: 24px; text-align: center;">
                    Ou copie e cole o link direto no seu navegador:<br />
                    <a href="{{loginUrl}}" style="color: #818cf8; word-break: break-all;">{{loginUrl}}</a>
                  </p>
                </div>
                <div class="footer">
                  <p>© {{DateTime.UtcNow.Year}} {{AppBrandName}}. Todos os direitos reservados.</p>
                  <p>Dúvidas ou suporte? Escreva para <a href="mailto:{{SupportEmail}}">{{SupportEmail}}</a>.</p>
                </div>
              </div>
            </body>
            </html>
            """;

        var text = $"""
            Olá, {recipientName}!

            O ambiente exclusivo da {companyName} no {AppBrandName} foi provisionado com sucesso!

            Dados de Acesso:
            - Empresa: {companyName}
            - Subdomínio: {subdomain}.admetricspro.com.br
            - E-mail de Login: {recipientEmail}
            - Plano: {tier} (14 dias de Trial gratuito)

            Para acessar seu painel agora, acesse o link:
            {loginUrl}

            Atenciosamente,
            Equipe {AppBrandName}
            """;

        return EmailMessage.Create(recipientEmail, subject, html, text).Value;
    }

    /// <inheritdoc />
    public EmailMessage RenderTrialNoticeEmail(
        string recipientEmail,
        string recipientName,
        string companyName,
        string subdomain,
        string? customDomain,
        TrialNoticeType noticeType,
        DateTime expirationUtc,
        double daysRemaining)
    {
        var loginUrl = BuildLoginUrl(subdomain, customDomain);
        var (subject, headline, urgencyText) = GetNoticeContent(noticeType, companyName, daysRemaining);

        var html = $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <title>{{subject}}</title>
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #0b0f19; color: #f1f5f9; margin: 0; padding: 0; }
                .wrapper { max-width: 600px; margin: 40px auto; background-color: #131b2e; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.5); }
                .header { background: linear-gradient(135deg, #e11d48 0%, #be123c 100%); padding: 32px; text-align: center; }
                .header h1 { margin: 0; font-size: 22px; font-weight: 700; color: #ffffff; }
                .content { padding: 36px 32px; }
                .content p { font-size: 15px; line-height: 1.6; color: #94a3b8; margin: 0 0 18px; }
                .content strong { color: #ffffff; }
                .alert-card { background-color: #1e1b4b; border-left: 4px solid #6366f1; padding: 18px; border-radius: 6px; margin: 20px 0; font-size: 14px; color: #c7d2fe; }
                .btn-container { text-align: center; margin: 32px 0 16px; }
                .btn { display: inline-block; background-color: #4f46e5; color: #ffffff !important; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-weight: 600; font-size: 15px; box-shadow: 0 4px 12px rgba(79, 70, 229, 0.35); }
                .footer { border-top: 1px solid #1e293b; padding: 24px 32px; text-align: center; font-size: 12px; color: #64748b; background-color: #0b0f19; }
                .footer a { color: #818cf8; text-decoration: none; }
              </style>
            </head>
            <body>
              <div class="wrapper">
                <div class="header">
                  <h1>{{headline}}</h1>
                </div>
                <div class="content">
                  <p>Olá, <strong>{{recipientName}}</strong>,</p>
                  <p>{{urgencyText}}</p>
                  
                  <div class="alert-card">
                    Ative seu plano definitivo para manter suas conexões de mídia, relatórios e automações funcionando sem interrupção.
                  </div>

                  <div class="btn-container">
                    <a href="{{loginUrl}}" class="btn" target="_blank">Ativar Meu Plano Definitivo</a>
                  </div>

                  <p style="font-size: 13px; color: #64748b; margin-top: 24px; text-align: center;">
                    Acesse seu painel através do endereço:<br />
                    <a href="{{loginUrl}}" style="color: #818cf8;">{{loginUrl}}</a>
                  </p>
                </div>
                <div class="footer">
                  <p>© {{DateTime.UtcNow.Year}} {{AppBrandName}}. Todos os direitos reservados.</p>
                  <p>Dúvidas sobre faturamento? Escreva para <a href="mailto:{{SupportEmail}}">{{SupportEmail}}</a>.</p>
                </div>
              </div>
            </body>
            </html>
            """;

        var text = $"""
            Olá, {recipientName}!

            {urgencyText}

            Para garantir a continuidade das operações de tráfego da {companyName}, ative seu plano definitivo:
            {loginUrl}

            Atenciosamente,
            Equipe {AppBrandName}
            """;

        return EmailMessage.Create(recipientEmail, subject, html, text).Value;
    }

    private static (string Subject, string Headline, string UrgencyText) GetNoticeContent(
        TrialNoticeType noticeType,
        string companyName,
        double daysRemaining)
    {
        return noticeType switch
        {
            TrialNoticeType.TrialReminder7Days => (
                $"[Aviso] Faltam 7 dias para o término do seu período de testes no {AppBrandName}",
                "Faltam 7 dias para o fim do seu Trial",
                $"Seu período de avaliação gratuita da <strong>{companyName}</strong> encerra em <strong>7 dias</strong>. Esperamos que você esteja aproveitando a gestão unificada de anúncios!"),

            TrialNoticeType.TrialReminder3Days => (
                $"[Importante] Faltam 3 dias de teste para a {companyName}",
                "Faltam apenas 3 dias de testes!",
                $"Faltam apenas <strong>3 dias</strong> para o encerramento do seu teste gratuito. Evite o congelamento de relatórios e a interrupção das métricas consolidando sua assinatura."),

            TrialNoticeType.TrialReminder1Day => (
                $"[Urgente] Resta 1 dia de trial gratuito no {AppBrandName}",
                "Seu período de testes expira em 1 dia!",
                $"Amanhã é o último dia do período de avaliação da <strong>{companyName}</strong>. Ative agora para manter seus clientes e squads operando sem bloqueios."),

            TrialNoticeType.TrialExpired => (
                $"[Ação Necessária] O período de testes da {companyName} expirou",
                "Seu período de avaliação gratuita expirou",
                $"O período de avaliação de 14 dias da <strong>{companyName}</strong> expirou. Para reativar o acesso total aos seus dados e automações, regularize sua assinatura agora."),

            _ => (
                $"Lembrete de ciclo de vida - {AppBrandName}",
                "Atualização da sua conta",
                $"Seu período de testes da <strong>{companyName}</strong> está próximo do encerramento.")
        };
    }

    private static string BuildLoginUrl(string subdomain, string? customDomain)
    {
        if (!string.IsNullOrWhiteSpace(customDomain))
        {
            return $"https://{customDomain.Trim().ToLowerInvariant()}/login";
        }

        var normalizedSubdomain = subdomain.Trim().ToLowerInvariant();
        return $"https://{normalizedSubdomain}.admetricspro.com.br/login";
    }

    /// <inheritdoc />
    public EmailMessage RenderSubscriptionConfirmationEmail(
        string recipientEmail,
        string companyName,
        SubscriptionTier tier,
        string billingCycle,
        decimal amount,
        DateTime paidAtUtc,
        DateTime expiresAtUtc)
    {
        var subject = $"[Confirmado] Assinatura do Plano {tier} ativada com sucesso - {AppBrandName}";
        var cycleDisplay = string.Equals(billingCycle, "Annual", StringComparison.OrdinalIgnoreCase) ? "Anual" : "Mensal";

        var html = $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <title>{{subject}}</title>
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #0b0f19; color: #f1f5f9; margin: 0; padding: 0; }
                .wrapper { max-width: 600px; margin: 40px auto; background-color: #131b2e; border: 1px solid #1e293b; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 25px rgba(0,0,0,0.5); }
                .header { background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 32px; text-align: center; }
                .header h1 { margin: 0; font-size: 22px; font-weight: 700; color: #ffffff; }
                .content { padding: 36px 32px; }
                .content p { font-size: 15px; line-height: 1.6; color: #94a3b8; margin: 0 0 18px; }
                .content strong { color: #ffffff; }
                .receipt-card { background-color: #0f172a; border: 1px solid #334155; border-radius: 8px; padding: 20px; margin: 20px 0; }
                .receipt-row { display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 14px; }
                .receipt-row:last-child { margin-bottom: 0; padding-top: 10px; border-top: 1px dashed #334155; font-weight: 600; }
                .footer { border-top: 1px solid #1e293b; padding: 24px 32px; text-align: center; font-size: 12px; color: #64748b; background-color: #0b0f19; }
              </style>
            </head>
            <body>
              <div class="wrapper">
                <div class="header">
                  <h1>Assinatura Confirmada!</h1>
                </div>
                <div class="content">
                  <p>Olá,</p>
                  <p>Confirmamos o pagamento e a ativação definitiva do plano da <strong>{{companyName}}</strong> no {{AppBrandName}}.</p>
                  
                  <div class="receipt-card">
                    <div class="receipt-row"><span>Plano:</span><strong>{{tier}}</strong></div>
                    <div class="receipt-row"><span>Ciclo de Faturamento:</span><strong>{{cycleDisplay}}</strong></div>
                    <div class="receipt-row"><span>Data de Pagamento:</span><strong>{{paidAtUtc:dd/MM/yyyy HH:mm}} UTC</strong></div>
                    <div class="receipt-row"><span>Próxima Renovação:</span><strong>{{expiresAtUtc:dd/MM/yyyy}}</strong></div>
                    <div class="receipt-row"><span>Total Liquidado:</span><strong style="color: #10b981;">R$ {{amount:N2}}</strong></div>
                  </div>

                  <p>Todos os limites operacionais e recursos da sua assinatura estão liberados.</p>
                </div>
                <div class="footer">
                  <p>{{AppBrandName}} — Plataforma de Gestão Unificada de Tráfego Pago.</p>
                </div>
              </div>
            </body>
            </html>
            """;

        var plainText = $"""
            Assinatura Confirmada - {AppBrandName}
            
            Confirmamos o pagamento e a ativação definitiva do plano da {companyName}.
            
            - Plano: {tier}
            - Ciclo: {cycleDisplay}
            - Total Pago: R$ {amount:N2}
            - Data de Liquidação: {paidAtUtc:dd/MM/yyyy HH:mm} UTC
            - Próxima Renovação: {expiresAtUtc:dd/MM/yyyy}
            
            {AppBrandName}
            """;

        return EmailMessage.Create(recipientEmail, subject, html, plainText).Value;
    }
}
