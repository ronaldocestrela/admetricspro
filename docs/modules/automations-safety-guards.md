# Módulo de Automações: Travas de Segurança (Overspending & Detector 404/500)

Este documento descreve a especificação técnica, modelos de dados, fluxos de contingência, contratos de API e despacho de alarmes das **Travas de Segurança Operacional**, correspondente à **Subfase 4.2** da plataforma **AdMetricsPro**.

---

## 1. Visão Geral e Princípios Operacionais

Em operações de tráfego pago de alto volume, falhas técnicas e desvios orçamentários causam prejuízos financeiros em minutos. As travas de segurança operam como *circuit breakers* automáticos que atuam proativamente para estancar desperdício de verba.

### As Duas Travas Centrais:
1. **`OverspendingGuard`:** Monitora continuamente o consumo diário em relação ao orçamento configurado (`DailyBudget`). Se o gasto acumulado no dia corrente ultrapassar **120%** do valor previsto, executa imediatamente a pausa da campanha via MediatR (`PauseCampaignCommand`) e dispara alertas urgentes em múltiplos canais.
2. **`LandingPageHealthChecker`:** Executa verificações periódicas nas URLs finais (`DestinationUrl`) dos anúncios ativos via requisições HTTP `HEAD` (com fallback para `GET`). Caso a URL retorne erro **4xx** (ex: 404 Not Found, 410 Gone), **5xx** (ex: 500 Internal Server Error, 502 Bad Gateway) ou sofra falha de resolução DNS/Timeout, executa a pausa imediata do anúncio (`PauseAdCommand`) e aciona alarme de emergência.

---

## 2. Arquitetura e Desacoplamento

### 2.1 Desacoplamento Inter-Módulos via MediatR
O módulo `Automations` **não** acessa repositórios nem `DbContext` de `Integrations`. As mutações de pausa são despachadas exclusivamente via comandos in-memory padronizados do Kernel Compartilhado:
- `BuildingBlocks.Application.Campaigns.Commands.PauseCampaignCommand`
- `BuildingBlocks.Application.Campaigns.Commands.PauseAdCommand`

### 2.2 Notificações Multi-Canal Resilientes
Os incidentes são notificados através do `SecurityAlertDispatcher` (`ISecurityAlertNotifier`), com disparo simultâneo para:
- **Slack:** Rich messages com Block Kit, badges coloridos de urgência e dados contextuais (gasto vs orçamento, URL com erro, status code);
- **WhatsApp:** Mensagens de texto urgentes estruturadas para o telefone do gestor do tenant;
- **E-mail:** E-mails transacionais com layout de alta prioridade via `IEmailSender`;
- **Webhook Genérico:** Payload JSON enviado via HTTP POST com cabeçalho de autenticação `X-Security-Alert-Token`.

**Tolerância a Falhas Parciais:** A falha de entrega em um canal (ex: timeout transitório no Slack) não impede o envio para WhatsApp ou E-mail, gerando um mapa granular de entrega (`IReadOnlyDictionary<SafetyAlertChannel, bool>`).

---

## 3. Catálogo de Endpoints RESTful (Web API)

Rota Base: `/api/v1/automations/guards`

| Método | Rota | Resumo OpenAPI / Scalar | Códigos HTTP |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/automations/guards/overspending/check` | Executa a trava de Overspending (>120% do orçamento diário) | `200 OK`, `400 BadRequest` |
| `POST` | `/api/v1/automations/guards/landing-pages/check` | Executa a verificação de saúde das Landing Pages (Detector 404/500) | `200 OK`, `400 BadRequest` |
| `GET` | `/api/v1/automations/guards/incidents?workspaceId={guid}` | Consulta o histórico de incidentes de segurança do workspace | `200 OK`, `400 BadRequest` |

---

## 4. Exemplos Práticos de Payloads

### 4.1 Execução da Trava de Overspending

#### Requisição (`POST /api/v1/automations/guards/overspending/check`):
```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "thresholdMultiplier": 1.20
}
```

#### Resposta de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "value": {
    "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "evaluatedCampaignsCount": 8,
    "violatedCampaignsCount": 1,
    "pausedCampaignsCount": 1,
    "incidents": [
      {
        "id": "e47ac10b-58cc-4372-a567-0e02b2c3d499",
        "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "guardType": 1,
        "severity": 2,
        "status": 2,
        "targetEntityName": "Campanha Black Friday - Performance Max",
        "targetEntityId": "a1b2c3d4-e5f6-7890-abcd-111111111111",
        "platform": "GoogleAds",
        "actionTaken": "Campanha Pausada Preventivamente",
        "reason": "Gasto diário de R$ 360,00 superou 120% do orçamento programado de R$ 250,00 (144.0% atingido).",
        "currentSpend": 360.00,
        "dailyBudget": 250.00,
        "httpStatusCode": null,
        "targetUrl": null,
        "detectedAtUtc": "2026-09-16T13:45:00Z",
        "resolvedAtUtc": null
      }
    ]
  },
  "error": {
    "code": "None",
    "description": ""
  }
}
```

---

### 4.2 Execução do Detector de Landing Pages Quebradas

#### Requisição (`POST /api/v1/automations/guards/landing-pages/check`):
```json
{
  "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### Resposta de Sucesso (`200 OK`):
```json
{
  "isSuccess": true,
  "isFailure": false,
  "value": {
    "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "evaluatedAdsCount": 24,
    "healthyAdsCount": 23,
    "brokenAdsCount": 1,
    "pausedAdsCount": 1,
    "incidents": [
      {
        "id": "f81d4fae-7dec-11d0-a765-00a0c91e6bf6",
        "workspaceId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "guardType": 2,
        "severity": 3,
        "status": 2,
        "targetEntityName": "Criativo Vídeo Promocional 01",
        "targetEntityId": "c3d4e5f6-a7b8-9012-cdef-333333333333",
        "platform": "MetaAds",
        "actionTaken": "Anúncio Pausado Preventivamente",
        "reason": "A Landing Page retornou código HTTP 404 (Not Found).",
        "currentSpend": null,
        "dailyBudget": null,
        "httpStatusCode": 404,
        "targetUrl": "https://lojaexemplo.com.br/produto-esgotado",
        "detectedAtUtc": "2026-09-16T13:46:12Z",
        "resolvedAtUtc": null
      }
    ]
  },
  "error": {
    "code": "None",
    "description": ""
  }
}
```

---

## 5. Tratamento de Casos de Borda

1. **Campanhas sem Orçamento Diário ou com Orçamento Zerado:** Campanhas com `DailyBudget == null` ou `DailyBudget <= 0` são ignoradas de forma segura, sem causar divisão por zero.
2. **Campanhas e Anúncios já Pausados:** Entidades em status `Paused` não são reprocessadas para evitar despachos desnecessários de comandos e mensagens duplicadas.
3. **Servidores Web com Bloqueio de HTTP HEAD:** Alguns provedores e CDNs (Cloudflare, Akamai) bloqueiam requisições HEAD retornando `405 Method Not Allowed`. O cliente efetua fallback imediato para HTTP `GET` lendo apenas os headers.
4. **Timeouts e Falhas de Resolução DNS:** O cliente HTTP possui timeout de 5 segundos. Falhas de conexão são capturadas e mapeadas em incidentes com justificativa clara sem lançar exceções não tratadas.
5. **URLs Malformadas ou Inválidas:** URLs inválidas são tratadas com mensagens de erro semânticas sem interrupção do lote de auditoria.
