# ADR 0049: Transporte SMTP da Plataforma com MailKit

## Status
Aceito

## Data
2026-11-17

## Contexto
O kernel de mensageria transacional (`IEmailSender`) já atendia boas-vindas, régua de trial, convites, relatórios e safety guards, conforme [ADR 0023](0023-transactional-emails-and-trial-lifecycle-messaging.md). A implementação de produção usava `System.Net.Mail.SmtpClient`, API legada sem timeout configurável, sem classificação de falhas transitórias, difícil de testar de forma isolada e com suporte limitado a TLS moderno.

Homologação e produção exigem envio confiável via SMTP corporativo (SendGrid, Amazon SES, Office 365, etc.) com STARTTLS/SSL, validação de configuração antes do envio e política de retry curta sem exceções de negócio.

## Decisão

1. **Substituir o transporte SMTP por MailKit 4.18.x** no `BuildingBlocks.Infrastructure`, mantendo o contrato público `IEmailSender` inalterado para todos os módulos consumidores.
2. **Separar orquestração e transporte:**
   - `SmtpEmailSender` valida `EmailOptions`, monta `MimeMessage` (`multipart/alternative`), aplica retry transitório (padrão: 1 nova tentativa) e retorna `Result`.
   - `ISmtpMailTransport` / `MailKitSmtpTransport` concentram conexão, autenticação opcional, envio e classificação de erros SMTP/rede.
3. **Configuração via seção `Email`** em `appsettings` com `Provider` (`InMemory` ou `Smtp`). Credenciais (`Email__UserName`, `Email__Password`) apenas em `.env` ([ADR 0018](0018-dotenv-configuration-and-secrets-management.md)).
4. **TLS configurável** com `SecureSocket` (`Auto`, `None`, `StartTls`, `SslOnConnect`), fallback para `EnableSsl` + porta quando omitido.
5. **TDD obrigatório** com `SmtpEmailSenderTests` usando transporte substituível (NSubstitute), sem dependência de servidor SMTP real nos testes unitários.

## Consequências

### Positivas
- Transporte testável, alinhado a práticas atuais de e-mail em .NET.
- Falhas de configuração retornam `Email.InvalidConfiguration` via `Result`, sem `throw` de regra de negócio.
- Retry limitado a falhas transitórias reduz perdas por indisponibilidade momentânea do provedor SMTP.
- Desenvolvimento e CI permanecem com `InMemory` por padrão.

### Negativas / Mitigações
- Nova dependência `MailKit` no kernel de infraestrutura; versão fixada em release sem aviso NU1902 do audit do SDK.
- Envio ainda síncrono dentro dos handlers HTTP; fila durável (outbox) permanece evolução futura se o volume ou a latência exigirem.

## Relação com decisões anteriores
Substitui **apenas o transporte SMTP** descrito no item 1 do [ADR 0023](0023-transactional-emails-and-trial-lifecycle-messaging.md). Eventos de domínio, `TenantNotificationLogs`, templates e endpoints permanecem válidos.