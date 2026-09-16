# Especificação Funcional & Técnica: Gerador de Relatórios Automatizados em White-Label

## 1. Visão Geral
O **Gerador de Relatórios Automatizados em White-Label** (Subfase 5.4) é o subsistema do AdMetricsPro encarregado de compilar, renderizar e despachar relatórios executivos de tráfego pago de forma multi-canal para os clientes finais da agência parceira.

### Pilares Fundamentais
1. **Identidade White-Label Estrita (Zero-Leakage):** Nenhuma menção, logotipo, rodapé ou metadados de domínio do AdMetricsPro são expostos nos artefatos. O relatório adota 100% as cores primária/secundária, logotipo, razão social, e-mail de suporte e domínio CNAME configurados pelo inquilino (Tenant).
2. **Saídas Híbridas (Web Interativa & PDF Vetorial):**
   - **Link Web Interativo Seguro (`/reports/shared/{token}` / `/r/{token}`):** Acesso com token criptográfico (24 bytes hex) sem exigir autenticação, com controle de expiração automático e layout responsivo de alto impacto.
   - **Documento PDF Vetorial Executivo:** Renderizado nativamente em .NET 10 (vetorial puro) pronto para impressão, arquivamento e anexo em disparos de e-mail.
3. **Agendador Periódico & Disparos Multi-Canal:**
   - Periodicidade configurável: Diária, Semanal (com dia da semana definido) ou Mensal (com dia do mês fixo).
   - Multi-canal: E-mail (com resumo HTML corporativo e PDF anexado) e WhatsApp corporativo (via webhook HTTP com resumo de KPIs e link web com token encurtado).
4. **Isolamento de Dados Multitenant (Database-per-Tenant):** Todas as regras de agendamento (`ReportSchedule`), relatórios gerados (`GeneratedReport`) e logs imutáveis de entrega (`ReportDispatchLog`) residem no `TenantDbContext` dedicado.

---

## 2. Modelagem de Dados e Ciclo de Vida

### 2.1 Entidades Principais (`BuildingBlocks.Domain.Reports`)
- **`ReportSchedule`:** Regra de recorrência com parâmetros de período (`ReportDateRangeType`), frequência (`ReportFrequency`), canais (`ReportDeliveryChannel`), formato (`ReportOutputFormat`), personalizações de título/notas e lista de destinatários com validação de formato (`ReportRecipient`).
- **`GeneratedReport`:** Registro imutável de relatório compilado, contendo o snapshot das métricas em JSON (`RenderModelJson`), snapshot de branding (`BrandingSnapshot`), token criptográfico de compartilhamento (`ShareToken`) e data limite de expiração (`ShareTokenExpiresAtUtc`).
- **`ReportDispatchLog`:** Registro de auditoria de cada tentativa de envio (canal, destino, status de sucesso, mensagem de erro e carimbo UTC).

### 2.2 Cálculo de Próxima Execução (`ReportSchedule.CalculateNextExecutionUtc`)
- **Diário (`Daily`):** Agendado para o dia corrente no horário `ScheduledTimeUtc`. Se o horário já tiver passado, avança para o dia seguinte ($D+1$).
- **Semanal (`Weekly`):** Encontra a próxima ocorrência do dia da semana especificado (`DayOfWeek`), respeitando o horário estipulado.
- **Mensal (`Monthly`):** Encontra o dia do mês configurado (`DayOfMonth`, padrão dia 1º). Caso o mês possua menos dias (ex.: dia 31 em fevereiro), ajusta automaticamente para o último dia válido daquele mês.

---

## 3. Contratos de API (OpenAPI & Scalar)

### 3.1 Listar Agendamentos: `GET /api/v1/workspaces/{workspaceId}/reports/schedules`
Retorna as regras de agendamento ativas e configuradas para o Workspace.

#### Exemplo de Resposta (HTTP 200 OK - Envelope Result)
```json
{
  "isSuccess": true,
  "value": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "workspaceId": "8b5e6790-2e4d-4e9b-b0b2-73a7c64b58e1",
      "name": "Relatório Semanal Diretoria",
      "frequency": "Weekly",
      "dayOfWeek": "Monday",
      "dayOfMonth": null,
      "scheduledTimeUtc": "08:00:00",
      "dateRangeType": "Last7Days",
      "outputFormat": "Both",
      "deliveryChannels": "Both",
      "customTitle": "Performance Semanal de Vendas",
      "customNotes": "Foco na redução de CPA com expansão no Google Ads.",
      "includeCopilotInsights": true,
      "includeTopCreatives": true,
      "includeChannelBreakdown": true,
      "includePacingSummary": true,
      "isActive": true,
      "lastExecutedAtUtc": "2026-09-14T08:00:00Z",
      "nextExecutionAtUtc": "2026-09-21T08:00:00Z",
      "createdAtUtc": "2026-09-01T12:00:00Z",
      "recipients": [
        {
          "name": "Diretoria Comercial",
          "channel": "Email",
          "destination": "diretoria@clientealpha.com"
        },
        {
          "name": "Gestor de Tráfego",
          "channel": "WhatsApp",
          "destination": "+5511999998888"
        }
      ]
    }
  ],
  "error": {
    "code": "None",
    "description": "None"
  }
}
```

---

### 3.2 Criar Agendamento: `POST /api/v1/workspaces/{workspaceId}/reports/schedules`

#### Payload de Entrada (JSON)
```json
{
  "name": "Relatório Mensal Consolidado",
  "frequency": 3,
  "dayOfWeek": null,
  "dayOfMonth": 1,
  "scheduledTimeUtc": "07:00:00",
  "dateRangeType": 4,
  "outputFormat": 3,
  "deliveryChannels": 1,
  "customTitle": "Fechamento Mensal de Performance",
  "customNotes": "Resultados auditados das campanhas com consolidação do MER.",
  "includeCopilotInsights": true,
  "includeTopCreatives": true,
  "includeChannelBreakdown": true,
  "includePacingSummary": true,
  "recipients": [
    {
      "name": "CFO",
      "channel": 1,
      "destination": "cfo@clientealpha.com"
    }
  ]
}
```

#### Exemplo de Resposta (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "value": "e2c05059-8692-4f32-8ea3-a79ff17f2bc2",
  "error": {
    "code": "None",
    "description": "None"
  }
}
```

---

### 3.3 Gerar Relatório Sob Demanda: `POST /api/v1/workspaces/{workspaceId}/reports/generate`
Compila imediatamente um relatório executivo com intervalo de datas personalizado e gera o token de compartilhamento.

#### Payload de Entrada (JSON)
```json
{
  "customTitle": "Auditoria Especial Black Friday",
  "dateRangeStartUtc": "2026-09-01T00:00:00Z",
  "dateRangeEndUtc": "2026-09-15T23:59:59Z",
  "outputFormat": 3,
  "includeCopilotInsights": true,
  "includeTopCreatives": true,
  "includeChannelBreakdown": true,
  "customNotes": "Relatório avulso emitido para reunião de alinhamento com a diretoria.",
  "tokenExpirationDays": 30
}
```

---

### 3.4 Acesso Público por Token: `GET /api/v1/public/reports/{token}`
Endpoint seguro e anônimo (`[AllowAnonymous]`) consumido pela rota de apresentação Blazor `/reports/shared/{token}` ou `/r/{token}`.

#### Exemplo de Resposta (HTTP 200 OK)
```json
{
  "isSuccess": true,
  "value": {
    "reportTitle": "Performance Semanal de Vendas",
    "workspaceName": "Cliente Alpha",
    "dateRangeStart": "2026-09-07T00:00:00Z",
    "dateRangeEnd": "2026-09-13T23:59:59Z",
    "branding": {
      "primaryColor": "#4F46E5",
      "secondaryColor": "#1E1B4B",
      "lightLogoUrl": "https://cdn.agenciaparceira.com.br/logo.png",
      "agencyName": "Agência Partner Growth",
      "supportEmail": "contato@partnergrowth.com.br",
      "supportPhone": "+5511988887777",
      "customDomain": "relatorios.partnergrowth.com.br"
    },
    "kpiSummary": {
      "totalSpend": 18450.00,
      "totalRevenue": 73800.00,
      "blendedRoas": 4.00,
      "blendedCpa": 19.50,
      "totalConversions": 946,
      "totalClicks": 24500,
      "totalImpressions": 612000
    },
    "channelBreakdown": [
      {
        "platform": "Meta Ads",
        "spend": 12000.00,
        "revenue": 50400.00,
        "roas": 4.20,
        "conversions": 650,
        "sharePercentage": 65.0
      },
      {
        "platform": "Google Ads",
        "spend": 6450.00,
        "revenue": 23400.00,
        "roas": 3.63,
        "conversions": 296,
        "sharePercentage": 35.0
      }
    ],
    "topCreatives": [],
    "copilotInsights": [
      "Meta Ads atingiu excelente eficiência de escala mantendo CPA estável.",
      "Campanhas de Google Ads Search com alta taxa de conversão no fundo de funil."
    ],
    "customNotes": "Recomendamos aumentar em 15% a alocação em criativos vencedores do Meta Ads.",
    "generatedAt": "2026-09-14T08:00:05Z",
    "isExpired": false,
    "shareToken": "3d59a2f7c00e6201b1e967a1492d"
  },
  "error": {
    "code": "None",
    "description": "None"
  }
}
```

---

## 4. Auditoria de Entregas & Casos de Falha
Cada disparo gera um registro em `ReportDispatchLog`:
- Se o canal for WhatsApp e o destinatário não contiver telefone válido internacional (`+DDI DDD NUMERO`), o sistema registra falha `WhatsApp.InvalidDestination` sem interromper o envio de e-mails para os demais destinatários.
- Se o token de compartilhamento tiver atingido sua data limite de validade, a API retorna sucesso com a flag `isExpired: true`, instruindo a UI pública a renderizar a tela de expiração com dados de contato da agência parceira.
