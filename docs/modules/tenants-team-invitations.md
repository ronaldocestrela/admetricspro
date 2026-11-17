# Convites permanentes de colaboradores

## Caso de uso

Administradores (Owner/Admin) convidam colaboradores pela tela `/settings/team` ou pelo modal FTUX. O sistema persiste o usuário **inativo**, registra um convite com token opaco (hash SHA-256) e envia e-mail com link para `/convite/{token}?subdomain={slug}`.

O colaborador define a senha na página pública; a API ativa a conta e libera login em `/login`.

## Endpoints autenticados

### POST `/api/v1/tenants/users`

**Entrada**

```json
{
  "fullName": "Carlos Oliveira",
  "email": "carlos@suaagencia.com.br",
  "role": "MediaManager",
  "phoneNumber": "11999990000"
}
```

**Retorno (`Result<InviteTenantUserResultDto>`)**

```json
{
  "isSuccess": true,
  "value": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "emailDispatched": true
  }
}
```

| Código de erro | HTTP |
| --- | --- |
| `TenantUser.EmailAlreadyInUse` | 409 |
| `TenantUser.OwnerNotAssignable` | 400 |
| `TenantUser.InviteForbidden` | 403 |
| `Auth.Forbidden` (sem permissão RBAC) | 403 |

### POST `/api/v1/tenants/users/{id}/invitations`

Reenvia convite para colaborador com status `PendingAcceptance`. Mesmo DTO de retorno do POST de convite.

### GET `/api/v1/tenants/users`

Lista colaboradores; `tenantUserDto.accountStatus` ∈ `Active`, `PendingAcceptance`, `Inactive`.

## Endpoints públicos

### GET `/api/v1/public/tenant-invitations/{token}?subdomain=vanguarda`

Retorna `TenantInvitationPreviewDto` (nome, e-mail, agência, expiração).

### POST `/api/v1/public/tenant-invitations/accept`

```json
{
  "token": "opaque-token",
  "subdomain": "vanguarda",
  "password": "SenhaForte@9",
  "confirmPassword": "SenhaForte@9"
}
```

Erros comuns: `TenantInvitation.NotFound`, `TenantUser.WeakPassword`, `TenantUser.PasswordMismatch`, `TenantUser.AlreadyActive`.

## Configuração

`appsettings`:

```json
"Tenants": {
  "Invitations": {
    "WebAppBaseUrl": "https://app.exemplo.com",
    "TokenValidityHours": 72
  }
}
```