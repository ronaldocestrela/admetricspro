# Documentação Técnica: Régua de Inadimplência e Bloqueio Progressivo (Subfase 3.2 - Dunning Engine)

## 1. Visão Geral

O módulo **Dunning Engine** implementa a governança de inadimplência e bloqueio funcional progressivo no SaaS AdMetricsPro. Em conformidade com o documento funcional (`docs/functions/Backoffice-Functions.md`), a plataforma previne o cancelamento involuntário (*churn*) e resguarda as contas de anúncio contra operações orçamentárias descontroladas por meio de uma régua escalonada de suspensão baseada em dias de atraso pós-vencimento:

- **D+0 até D+2 (Tolerância / Grace Period):** Período de carência inicial. Todas as funcionalidades operam plenamente; cobranças automáticas de retentativa e notificações preventivas ocorrem em background.
- **D+3 até D+6 (Estágio 1 - Desativação de Automações):** O motor de automação cross-network e pausas automáticas de regras são desativados para o tenant (`DunningStage.AutomationsDisabled`). Visualização de relatórios e autenticação permanecem liberadas.
- **D+7 até D+13 (Estágio 2 - Bloqueio de Relatórios):** Consultas a relatórios analíticos, dashboards de performance, métricas de atribuição e MER são bloqueadas (`DunningStage.ReportsBlocked`).
- **D+14+ (Estágio 3 - Suspensão Total / Bloqueio de Login):** A organização é marcada com status `TenantStatus.Suspended`, todos os acessos são revogados e novos logins são bloqueados (`DunningStage.LoginBlocked`).
- **Regularização Financeira:** Ao quitar os valores devidos, o tenant é restaurado imediatamente para `DunningStage.None` e `TenantStatus.Active`.

---

## 2. Diagrama de Arquitetura e Fluxo de Execução

```mermaid
flowchart TD
    subgraph Trigger ["Disparo"]
        BG[DunningBackgroundService<br/>BackgroundService In-Memory]
        API[POST /api/v1/billing/dunning/execute<br/>BillingController]
    end

    subgraph App ["Aplicação (Master.Application)"]
        CMD[ExecuteDunningCycleCommand]
        SVC[IDunningEngineService<br/>DunningEngineService]
        PUB[IPublisher (MediatR)]
        EVT[TenantGracePeriodExceededEventHandler]
    end

    subgraph Domain ["Domínio (Master.Domain)"]
        POL[DunningPolicy<br/>D+3, D+7, D+14]
        AGG[Tenant Aggregate<br/>EvaluateDunningStage]
        DEVT[TenantGracePeriodExceededEvent]
    end

    subgraph Data ["Persistência (MasterDb)"]
        REPO[ITenantRepository<br/>GetTenantsForDunningEvaluationAsync]
        UOW[IUnitOfWork]
        SQL[(SQL Server MasterCatalog)]
    end

    BG -->|A cada 24h| SVC
    API -->|Manual| CMD --> SVC
    SVC -->|Consulta inadimplentes| REPO --> SQL
    SVC -->|Para cada tenant| AGG --> POL
    AGG -->|Transição / Carência extrapolada| DEVT
    SVC -->|Commit transacional| UOW --> SQL
    SVC -->|Publica evento| PUB --> EVT
```

---

## 3. Especificação dos Endpoints (OpenAPI / Scalar UI)

### 3.1 Executar Ciclo Imediato de Dunning

- **Rota:** `POST /api/v1/billing/dunning/execute`
- **Sumário OpenAPI:** `Executa o ciclo da régua de inadimplência e suspensão progressiva (Dunning Engine)`
- **Tags:** `Billing`

#### Payload de Entrada (Opcional)
```json
{
  "referenceDateUtc": "2026-09-17T12:00:00Z"
}
```
> *Nota: Quando `referenceDateUtc` for nulo ou omitido, o sistema utiliza o horário atual (`DateTime.UtcNow`).*

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
    "evaluatedCount": 15,
    "transitionsCount": 4,
    "suspendedCount": 1,
    "unchangedCount": 11,
    "executedAtUtc": "2026-09-17T12:00:00Z"
  }
}
```

#### Resposta de Erro (HTTP 400 Bad Request)
```json
{
  "isSuccess": false,
  "isFailure": true,
  "error": {
    "code": "Dunning.ExecutionFailed",
    "description": "Falha ao persistir transições de estágio.",
    "type": 1
  },
  "value": null
}
```

---

## 4. Contratos de Domínio e Política de Permissões

### Tabela de Permissões por Estágio

| Estágio | Limiar | Automações Permitidas? | Relatórios Permitidos? | Login Permitido? | Status do Tenant |
| :--- | :--- | :---: | :---: | :---: | :---: |
| `None` (0) | D+0 a D+2 | **Sim** | **Sim** | **Sim** | `Active` |
| `AutomationsDisabled` (1) | D+3 a D+6 | **Não** | **Sim** | **Sim** | `Active` |
| `ReportsBlocked` (2) | D+7 a D+13 | **Não** | **Não** | **Sim** | `Active` |
| `LoginBlocked` (3) | D+14+ | **Não** | **Não** | **Não** | `Suspended` |

### Evento de Domínio `TenantGracePeriodExceededEvent`

Implementa `IDomainEvent` do kernel compartilhado:
```csharp
public sealed record TenantGracePeriodExceededEvent(
    TenantId TenantId,
    DunningStage PreviousStage,
    DunningStage CurrentStage,
    int DaysOverdue,
    DateTime DueDateUtc) : IDomainEvent;
```

---

## 5. Configuração do Background Service

Em `appsettings.json`:
```json
{
  "Dunning": {
    "Enabled": true,
    "IntervalMinutes": 1440
  }
}
```

- **`Enabled`**: ativa ou desativa o timer do background service.
- **`IntervalMinutes`**: frequência de execução (padrão de 1440 minutos = 24 horas).
