# ADR 0023: Mensageria Transacional e Régua Automatizada de Ciclo de Vida do Trial

## Status
Aceito

## Data
2026-09-07

## Contexto
No fluxo de onboarding self-service de agências e gestão multitenant:
1. Ao concluir o provisionamento do banco dedicado de um novo inquilino, o gestor necessita receber imediatamente as credenciais e o link direto para o seu domínio/subdomínio exclusivo de login (`https://{subdomain}.admetricspro.com.br/login`).
2. Clientes em período de teste gratuito (*Trial*) necessitam de acompanhamento proativo de ciclo de vida com avisos aos 7 dias, 3 dias e 1 dia antes do encerramento, além da notificação de expiração, prevenindo surpresas operacionais e incentivando o upgrade para plano pago.
3. Notificações por e-mail devem ser resilientes, não gerando duplicações indesejadas (idempotência auditada) e permitindo reenvio manual sob demanda via API autenticada.
4. O envio de e-mails precisa ser agnóstico de infraestrutura (suportando provedores SMTP padrão, serviços transacionais dedicados e mock in-memory para testes automatizados).

## Decisão
1. **Kernel Compartilhado de Mensageria (`BuildingBlocks.Application/Emails` & `BuildingBlocks.Infrastructure/Emails`):**
   - Abstração `IEmailSender` com contrato `SendEmailAsync(EmailMessage, CancellationToken)` retornando `Result`.
   - Implementação `SmtpEmailSender` para envio em homologação/produção e `InMemoryEmailSender` para testes de integração e ambientes de desenvolvimento locais.
   - Configurações via `EmailOptions` com suporte a variáveis de ambiente (`EMAIL__HOST`, `EMAIL__USERNAME`, `EMAIL__APIKEY`, etc.).
2. **Registro e Auditoria Central no `MasterDb` (`TenantNotificationLog`):**
   - Criação da tabela `TenantNotificationLogs` no banco mestre, vinculada ao `TenantId`, armazenando tipo de aviso (`TrialNoticeType`), destinatário, assunto, status de sucesso, data UTC e payload/mensagem de erro.
   - Índice composto `IX_TenantNotificationLogs_TenantId_NoticeType_SentAtUtc` garantindo idempotência e consulta ultrarrápida da régua.
3. **E-mail de Boas-Vindas Automatizado via Eventos de Domínio:**
   - Disparo assíncrono desacoplado via MediatR ao publicar `TenantProvisionedEvent` durante a conclusão do `TenantProvisioningService`.
   - Handler `TenantProvisionedSendWelcomeEmailEventHandler` renderiza template HTML corporativo com cores da marca, link do subdomínio e registra auditoria.
   - Endpoint `POST /api/v1/tenants/{id}/resend-welcome-email` permitindo que operadores de suporte ou a própria agência solicitem reenvio do e-mail.
4. **Motor de Avaliação e Régua de Trial (`TrialNotificationEngineService`):**
   - Serviço hospedado em segundo plano (`TrialNoticeBackgroundService`) executando a cada 60 minutos (configurável) com avaliação de janelas temporais baseadas em `TrialNoticePolicy`.
   - Disparo de e-mails em D-7, D-3, D-1 e Expiração com verificação contra `TenantNotificationLog` para prevenir disparos duplicados no mesmo ciclo de vida.
   - Endpoint manual `POST /api/v1/billing/trial-notices/execute` para gatilhos operacionais de rotinas agendadas (ex: cron jobs externos) e testes pontuais.

## Consequências
- **Positivas:**
  - Experiência fluida de ativação para a agência: login e orientações disponíveis no e-mail em segundos após o cadastro.
  - Mitigação de churn e aumento de taxa de conversão do trial para planos comerciais através da régua regressiva D-7, D-3 e D-1.
  - Rastreabilidade integral: histórico completo de comunicações registradas no catálogo central.
  - Zero acoplamento de banco: o serviço consome dados do `MasterDbContext` sem violar os limites dos bancos dedicados dos inquilinos.
- **Mitigações:**
  - Falhas transitórias no provedor SMTP são registradas como falhas auditadas (`IsSuccess = false`) sem interromper o processamento dos demais inquilinos da fila.
