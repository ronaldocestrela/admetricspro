# Backoffice MasterDb - Provisionamento de Tenant

## Objetivo

Documentar o fluxo da Subfase 1.2 para provisionamento dinamico de banco de tenant com persistencia no MasterDb.

## Entidade Tenant (MasterDb)

Campos principais:

- Id (TenantId - GUID fortemente tipado)
- CompanyName (string, max 200)
- Cnpj (string, 14 dígitos numéricos, único)
- Subdomain (string, max 80, único)
- EncryptedConnectionString (string, max 2000, AES-256)
- Status (TenantStatus: Trial, Active, Suspended, Cancelled)
- Tier (SubscriptionTier: Trial, Starter, Pro, Enterprise)
- SubscriptionExpiresAtUtc (DateTime?, nullable)
- CreatedAtUtc (DateTime, UTC)

Restrições:

- Cnpj único
- Subdomain único

Métodos de Ciclo de Vida:

- `UpgradeSubscription(SubscriptionTier newTier, DateTime? expiresAtUtc)`
- `ExtendTrial(DateTime newExpirationUtc)`
- `Suspend(string reason)`
- `Reactivate()`

## Fluxo de Provisionamento

1. Validar dados de entrada do tenant.
2. Verificar conflitos de CNPJ e subdominio no MasterDb.
3. Gerar nome fisico do banco do tenant a partir do subdominio sanitizado.
4. Criar banco no SQL Server, se inexistente.
5. Aplicar schema inicial do tenant no banco dedicado.
6. Criptografar a connection string do tenant com AES-256.
7. Persistir tenant no MasterDb e executar commit via UnitOfWork.

## Exemplo de Entrada

```json
{
  "companyName": "Agencia Alfa",
  "cnpj": "12345678000190",
  "subdomain": "agencia-alfa"
}
```

## Exemplo de Saida (Sucesso)

```json
{
  "isSuccess": true,
  "value": "tenant-id-guid"
}
```

## Exemplo de Saida (Falha)

```json
{
  "isSuccess": false,
  "error": {
    "code": "Tenant.DatabaseAlreadyExists",
    "description": "A database already exists for the requested tenant."
  }
}
```

## Erros de Negocio Mapeados

- Tenant.InvalidCnpj
- Tenant.InvalidSubdomain
- Tenant.CompanyNameRequired
- Tenant.SubdomainAlreadyExists
- Tenant.CnpjAlreadyExists
- Tenant.DatabaseAlreadyExists
