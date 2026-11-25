# Backoffice — Dead-letters de mensageria

Consulta e reprocessamento de linhas `IntegrationOutboxMessage` em status `DeadLetter` no SQL (catálogo Master e bancos de tenants ativos/trial). Não consulta a fila `admetricspro.dead-letter` do broker diretamente.

## Endpoints (Web API)

| Método | Rota | Retorno |
| :--- | :--- | :--- |
| `GET` | `/api/v1/admin/messaging/dead-letters?limit=50` | `Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>` |
| `POST` | `/api/v1/admin/messaging/dead-letters/{eventId}/reprocess` | `Result<Guid>` (novo `EventId`) |

### Corpo do reprocessamento

```json
{
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

Omita `tenantId` ou envie `null` para linhas do catálogo Master.

### Exemplo de item na listagem

```json
{
  "isSuccess": true,
  "value": [
    {
      "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "messageType": "catalog.tenant-welcome-email.v1",
      "tenantId": null,
      "attemptCount": 5,
      "lastErrorCode": "Email.SendFailed",
      "createdAtUtc": "2026-11-25T12:00:00Z"
    }
  ],
  "error": { "code": "", "description": "" }
}
```

## Regras

- Reprocessar **não** edita JSON na tela: `IntegrationDeadLetterReprocessFactory` clona o payload, gera novo `eventId` e define `correlationId` = `eventId` original.
- A linha morta permanece `DeadLetter` para auditoria.
- Segunda reprocessamento enquanto existir filha `Pending`/`InProgress` com o mesmo `correlationId` → `Messaging.ReprocessPending` (HTTP 409).

## UI

Rota Blazor: `/messaging-dead-letters` (SuperAdmin). Cliente: `IMessagingDeadLetterClientService` → HTTP tipado, sem MassTransit no frontend.