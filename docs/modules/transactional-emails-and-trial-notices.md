# Documentação Técnica: Mensageria Transacional e Régua de Ciclo de Vida do Trial (Subfase 6.1)

## 1. Visão Geral

O módulo **Mensageria Transacional & Régua de Trial** implementa o sistema de disparo de e-mails transacionais e a gestão automatizada do ciclo de vida de testes gratuitos no SaaS AdMetricsPro:

1. **E-mail de Boas-Vindas Automatizado (Welcome Email):**
   - Disparado imediatamente após a conclusão do provisionamento do banco dedicado de um novo inquilino (`TenantProvisionedEvent`).
   - Apresenta as credenciais de acesso, plano contratado, orientações de primeiros passos e um botão de ação com link direto para o subdomínio exclusivo de login da agência: `https://{subdomain}.admetricspro.com.br/login` (ou domínio CNAME customizado caso configurado).
   - Disponibiliza endpoint de reenvio manual para suporte e autoatendimento: `POST /api/v1/tenants/{id}/resend-welcome-email`.

2. **Régua Automatizada de Ciclo de Vida de Trial:**
   - Avalia inquilinos ativos em período de teste (`TenantStatus.Trial` ou `SubscriptionTier.Trial`) e dispara notificações preventivas:
     - **D-7 (Aviso de 7 dias):** Disparado quando faltam entre 4 e 7 dias para o encerramento do trial.
     - **D-3 (Aviso de 3 dias):** Disparado quando faltam entre 2 e 3 dias para o encerramento do trial.
     - **D-1 (Aviso de 24 horas):** Disparado quando resta 1 dia para o encerramento do trial.
     - **Trial Expirado:** Disparado imediatamente após a expiração do trial, orientando a regularização e upgrade de plano.
   - Idempotência rigorosa garantida através da tabela de auditoria `TenantNotificationLogs` no `MasterDb`.

3. **Abstração Desacoplada de E-mails (`BuildingBlocks.Application/Emails`):**
   - Suporte transparente a envio via SMTP corporativo (`SmtpEmailSender`) e modo in-memory (`InMemoryEmailSender`) para testes automatizados.
   - Templates HTML modernos, responsivos e acessíveis com paleta corporativa slate/indigo e fallback automático em texto plano.

---

## 2. Diagrama de Arquitetura e Fluxo

```mermaid
flowchart TD
    subgraph Gatilhos ["Gatilhos de Notificação"]
        PROV[TenantProvisioningService<br/>TenantProvisionedEvent]
        BG[TrialNoticeBackgroundService<br/>BackgroundService a cada 1h]
        API_RESEND[POST /api/v1/tenants/{id}/resend-welcome-email]
        API_TRIAL[POST /api/v1/billing/trial-notices/execute]
    end

    subgraph Aplicacao ["Aplicação (Master.Application)"]
        H_PROV[TenantProvisionedSendWelcomeEmailEventHandler]
        H_RESEND[ResendWelcomeEmailCommandHandler]
        CMD_TRIAL[ExecuteTrialNoticeCycleCommandHandler]
        ENG[TrialNotificationEngineService]
        TPL[TransactionalEmailTemplateRenderer]
    end

    subgraph Dominio ["Domínio (Master.Domain)"]
        POL[TrialNoticePolicy<br/>D-7, D-3, D-1, Expirado]
        LOG_ENT[TenantNotificationLog Entity]
    end

    subgraph Infra ["Infraestrutura & Persistência"]
        SENDER[IEmailSender<br/>SmtpEmailSender / InMemoryEmailSender]
        REPO_TENANT[ITenantRepository]
        REPO_LOG[ITenantNotificationLogRepository]
        MASTER_DB[(SQL Server MasterCatalog)]
    end

    PROV -->|Publica evento| H_PROV
    API_RESEND --> H_RESEND
    BG --> ENG
    API_TRIAL --> CMD_TRIAL --> ENG

    H_PROV --> TPL
    H_PROV --> SENDER
    H_PROV --> REPO_LOG

    H_RESEND --> REPO_TENANT
    H_RESEND --> TPL
    H_RESEND --> SENDER
    H_RESEND --> REPO_LOG

    ENG --> REPO_TENANT
    ENG --> POL
    ENG --> REPO_LOG
    ENG --> TPL
    ENG --> SENDER
    ENG --> MASTER_DB
```

---

## 3. Especificação dos Endpoints (OpenAPI / Scalar UI)

### 3.1 Reenviar E-mail de Boas-Vindas

- **Rota:** `POST /api/v1/tenants/{tenantId}/resend-welcome-email`
- **Sumário OpenAPI:** `Reenvia o e-mail transacional de boas-vindas com o link de login exclusivo do inquilino`
- **Tags:** `Tenants`

#### Resposta de Sucesso (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": null,
    "description": null,
    "type": 0
  },
  "value": true
}
```

#### Resposta de Inquilino Não Encontrado (HTTP 404 Not Found)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Tenant.NotFound",
    "description": "Inquilino com ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 não foi encontrado.",
    "type": 3
  },
  "value": false
}
```

---

### 3.2 Executar Ciclo da Régua de Trial

- **Rota:** `POST /api/v1/billing/trial-notices/execute`
- **Sumário OpenAPI:** `Executa a avaliação da régua de ciclo de vida do trial e despacha e-mails preventivos`
- **Tags:** `Billing`

#### Payload de Entrada (Opcional)
```json
{
  "referenceDateUtc": "2026-09-17T12:00:00Z"
}
```
> *Nota: Quando `referenceDateUtc` for nulo ou omitido, o motor assume o horário UTC atual.*

#### Resposta de Sucesso (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "isFailure": false,
  "error": {
    "code": null,
    "description": null,
    "type": 0
  },
  "value": {
    "evaluatedCount": 12,
    "sevenDayNoticesSent": 4,
    "threeDayNoticesSent": 3,
    "oneDayNoticesSent": 2,
    "expiredNoticesSent": 1,
    "totalNoticesSent": 10,
    "failuresCount": 0,
    "executedAtUtc": "2026-09-17T12:00:00Z"
  }
}
```

---

## 4. Auditoria e Idempotência (`TenantNotificationLogs`)

Cada comunicação transacional gera uma entrada auditável na tabela `TenantNotificationLogs` no `MasterDb`:

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `uniqueidentifier` | Chave primária |
| `TenantId` | `uniqueidentifier` | Identificador do inquilino (FK para `Tenants`) |
| `NoticeType` | `int` | Tipo de notificação (`WelcomeEmail`, `TrialReminder7Days`, `TrialReminder3Days`, `TrialReminder1Day`, `TrialExpired`) |
| `RecipientEmail` | `nvarchar(256)` | E-mail do destinatário |
| `Subject` | `nvarchar(300)` | Assunto do e-mail |
| `IsSuccess` | `bit` | Indicador se o envio no provedor SMTP foi bem-sucedido |
| `SentAtUtc` | `datetime2` | Timestamp UTC do envio |
| `ErrorMessage` | `nvarchar(2000)` | Mensagem de erro caso `IsSuccess` seja `false` |

Um índice composto não agrupado (`IX_TenantNotificationLogs_TenantId_NoticeType_SentAtUtc`) garante alta performance nas consultas de verificação de idempotência da régua.
